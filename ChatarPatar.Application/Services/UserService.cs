using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.RepositoryContracts;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security;
using ChatarPatar.Common.Security.SecurityContracts;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;

namespace ChatarPatar.Application.Services;

internal class UserService : IUserService
{
    private readonly IRepositoryManager _repositories;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;
    private readonly AuthTokenStrategyFactory _tokenStrategyFactory;
    private readonly IMapper _mapper;
    private readonly TokenSettings _tokenSettings;

    public UserService(IRepositoryManager repositories, IExternalServiceManager externalServiceManager, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IMapper mapper, AuthTokenStrategyFactory tokenStrategyFactory, IOptions<TokenSettings> tokenSettings)
    {
        _repositories = repositories;
        _externalServiceManager = externalServiceManager;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _mapper = mapper;
        _tokenStrategyFactory = tokenStrategyFactory;
        _tokenSettings = tokenSettings.Value;
    }

    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    #region Auth

    public async Task<LoginResponseDto> LoginUserAsync(UserLoginDto user)
    {
        _validationService.Validate<UserLoginDto>(user);

        var identifier = user.Identifier.Trim().ToLower();

        var existUser = await _repositories.UserRepository.GetUserByIdentifierAsync(email: identifier, username: identifier);

        if (existUser is null || !PasswordHasher.VerifyPassword(hashedPassword: existUser.PasswordHash, providedPassword: user.Password))
            throw new InvalidDataAppException("Credentials");

        var strategy = GetTokenStrategy();
        return await AuthenticateUser(strategy, existUser);
    }

    public async Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user)
    {
        _validationService.Validate<UserRegisterDto>(user);

        var username = user.Username.Trim().ToLower();
        var email = user.Email.Trim().ToLower();

        var existingUser = await _repositories.UserRepository.GetUserByIdentifierAsync(email: email, username: username);

        if (existingUser != null)
            throw new DuplicateEntryAppException(existingUser.Username == username ? "Username is already registered" : "Email is already registered");

        user.Username = username;
        user.Email = email;

        var userEntity = _mapper.Map<User>(user);
        userEntity.PasswordHash = PasswordHasher.HashPassword(user.Password);

        var userStatusEntity = new UserStatus
        {
            User = userEntity,
            Status = PresenceStatusEnum.Online
        };

        if (!string.IsNullOrWhiteSpace(user.InviteToken))
        {
            // ── INVITE TOKEN PATH ──────────────────────────────────────────────
            var hashedToken = _tokenService.HashToken(user.InviteToken.Trim());

            var invite = await _repositories.OrganizationInviteRepository
                .GetPendingByToken(hashedToken)
                .FirstOrDefaultAsync();

            if (invite is null)
                throw new InvalidDataAppException("Invite token is invalid or has expired");

            if (invite.Email != email)
                throw new InvalidDataAppException("This invite was sent to a different email address");

            var membershipEntity = new OrganizationMember
            {
                User = userEntity,
                OrgId = invite.OrganizationId,
                Role = invite.Role,
                InvitedByUserId = invite.CreatedBy,
                JoinedAt = DateTime.UtcNow,
                CreatedByUser = userEntity,
            };

            invite.IsUsed = true;
            invite.UsedAt = DateTime.UtcNow;
            invite.UsedByUser = userEntity;

            await _repositories.UserRepository.AddAsync(userEntity);
            await _repositories.OrganizationMemberRepository.AddAsync(membershipEntity);
            await _repositories.UserStatusRepository.AddAsync(userStatusEntity);

            // One SaveChangesAsync = one transaction for invite path
            await _repositories.UnitOfWork.SaveChangesAsync();

            var strategy = GetTokenStrategy();
            return await AuthenticateUser(strategy, userEntity);
        }
        else
        {
            // ── NEW ORG PATH ───────────────────────────────────────────────────

            var orgName = user.NewOrg!.Name.Trim();
            var slug = user.NewOrg!.Slug.Trim();

            var slugExists = await _repositories.OrganizationRepository
                .AnyAsync(o => o.Slug == slug);

            if (slugExists)
                throw new DuplicateEntryAppException("Organization slug is already taken");

            await using var transaction = await _repositories.UnitOfWork.BeginTransactionAsync();
            try
            {
                // Step 1 — insert User + UserStatus, get real DB-generated Id back
                await _repositories.UserRepository.AddAsync(userEntity);
                await _repositories.UserStatusRepository.AddAsync(userStatusEntity);
                await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

                // Step 2 — build Org + OrgMember with the now-real UserId
                var orgEntity = new Organization
                {
                    Name = orgName,
                    Slug = slug,
                    CreatedBy = userEntity.Id,
                };

                orgEntity.OrganizationMembers.Add(new OrganizationMember
                {
                    UserId = userEntity.Id,
                    Role = OrganizationRoleEnum.OrgOwner,
                    JoinedAt = DateTime.UtcNow,
                    CreatedBy = userEntity.Id,
                });

                await _repositories.OrganizationRepository.AddAsync(orgEntity);
                await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

                await transaction.CommitAsync();

                // Only write audit logs AFTER commit succeeds.
                _repositories.UnitOfWork.FlushPendingAuditLogs();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var strategy = GetTokenStrategy();
            return await AuthenticateUser(strategy, userEntity);
        }
    }

    public async Task<LoginResponseDto> RefreshAuthToken()
    {
        var strategy = GetTokenStrategy();
        var refreshToken = strategy.GetRefreshToken();

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new NotFoundAppException("Refresh token");

        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _repositories.RefreshTokenRepository.FindActiveRefreshToken(token: tokenHash).AsNoTracking().FirstOrDefaultAsync();

        if (storedToken == null)
            throw new InvalidDataAppException("Refresh token");

        var user = await _repositories.UserRepository.GetById(storedToken.UserId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        return await AuthenticateUser(strategy, user, storedToken);
    }

    public async Task LogoutUser()
    {
        var strategy = GetTokenStrategy();
        var refreshToken = strategy.GetRefreshToken();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var storedToken = await _repositories.RefreshTokenRepository.FindActiveRefreshToken(token: tokenHash).FirstOrDefaultAsync();

            if (storedToken != null)
            {
                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;
                storedToken.UpdatedAt = DateTime.UtcNow;

                await _repositories.UnitOfWork.SaveChangesAsync();
            }
        }

        strategy.ClearTokens();
    }

    public async Task RevokeAllUserSessions(Guid? userId = null)
    {
        var strategy = GetTokenStrategy();

        userId = userId ?? Guid.Parse(_httpContext!.GetUserId());

        var tokens = await _repositories.RefreshTokenRepository.GetActiveRefreshTokensByUserId(userId: (Guid)userId).ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.UpdatedAt = DateTime.UtcNow;
        }
        await _repositories.UnitOfWork.SaveChangesAsync();

        strategy.ClearTokens();
    }

    #endregion

    #region Basic User Operations

    public async Task<AuthUserDto> GetCurrentUserAsync()
    {
        var userId = Guid.Parse(_httpContext!.GetUserId());

        var user = await _repositories.UserRepository.GetById(userId)
            .ProjectTo<AuthUserDto>(_mapper.ConfigurationProvider)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("Login User data");

        return user;
    }

    public async Task UpdateAvatarAsync(Guid userId, IFormFile file)
    {
        var fileType = file.ValidateFile(FileUsageContextEnum.Avatar);

        var user = await _repositories.UserRepository.GetById(userId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        var publicId = string.Empty;

        if (user.AvatarFileId != null)
        {
            var userAvatarFile = await _repositories.FileRepository.GetByIdAsync((Guid)user.AvatarFileId).FirstOrDefaultAsync();

            if (userAvatarFile == null)
                throw new NotFoundAppException("Exist User Avatar file data");

            userAvatarFile.IsDeleted = true;

            publicId = userAvatarFile.PublicId;
        }
        else
            publicId = CloudinaryPublicId.UserAvatar(user.Id);

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(file, CloudinaryPath.Users().Avatars(), publicId);

        user.AvatarFile = new FileEntity()
        {
            UploadedByUserId = user.Id,
            UserId = user.Id,
            UsageContext = FileUsageContextEnum.Avatar,

            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            ThumbnailUrl = uploadResult.ThumbnailUrl,

            SizeInBytes = file.Length,
            OriginalName = file.FileName,
            MimeType = file.ContentType,
            FileType = fileType,
        };

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Private section

    private async Task<LoginResponseDto> AuthenticateUser(IAuthTokenStrategy strategy, User user, RefreshToken? entity = null)
    {
        string token = _tokenService.CreateToken(email: user.Email, id: user.Id, name: user.Name);
        var accessTokenResponse = strategy.SetAccessToken(token);

        var refreshTokenResponse = await IssueRefreshTokenAsync(strategy, user, entity);

        return new LoginResponseDto()
        {
            AccessToken = accessTokenResponse,
            RefreshToken = refreshTokenResponse,
            ExpiresIn = _tokenSettings.TokenExpirationMinutes * 60,
            TokenType = "Bearer"
        };
    }

    private async Task<string?> IssueRefreshTokenAsync(IAuthTokenStrategy strategy, User user, RefreshToken? entity = null)
    {
        var refreshToken = _tokenService.GenerateRefreshToken();
        var deviceInfo = _httpContext!.GetDeviceInfo();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashToken(refreshToken),
            Device = deviceInfo.Device,
            Browser = deviceInfo.Browser,
            OperatingSystem = deviceInfo.OS,
            IPAddress = _httpContext!.GetClientIp(),
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpirationDays)
        };

        if (entity != null)
        {
            refreshTokenEntity.Id = entity.Id;
            refreshTokenEntity.UpdatedAt = DateTime.UtcNow;
            refreshTokenEntity.CreatedAt = entity.CreatedAt;

            _repositories.RefreshTokenRepository.Update(entity, refreshTokenEntity);
        }
        else
        {
            var tokensToRevoke = await _repositories.RefreshTokenRepository
            .FindByCondition(x => x.UserId == user.Id && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(_tokenSettings.MaxSessions - 1)
            .ToListAsync();

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _repositories.RefreshTokenRepository.AddAsync(refreshTokenEntity);
        }

        await _repositories.UnitOfWork.SaveChangesAsync();

        var refreshTokenResponse = strategy.SetRefreshToken(refreshToken);

        return refreshTokenResponse;
    }

    private IAuthTokenStrategy GetTokenStrategy() => _tokenStrategyFactory.Resolve();

    #endregion
}
