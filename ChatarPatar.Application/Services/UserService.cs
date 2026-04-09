using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security;
using ChatarPatar.Common.Security.SecurityContracts;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    private readonly IEmailNotificationService _emailNotificationService;

    public UserService(IRepositoryManager repositories, IExternalServiceManager externalServiceManager, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IMapper mapper, AuthTokenStrategyFactory tokenStrategyFactory, IOptions<TokenSettings> tokenSettings, IEmailNotificationService emailNotificationService)
    {
        _repositories = repositories;
        _externalServiceManager = externalServiceManager;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _mapper = mapper;
        _tokenStrategyFactory = tokenStrategyFactory;
        _tokenSettings = tokenSettings.Value;
        _emailNotificationService = emailNotificationService;
    }

    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    #region Auth

    public async Task<LoginResponseDto> LoginUserAsync(UserLoginDto user)
    {
        await _validationService.ValidateAsync<UserLoginDto>(user);

        var identifier = user.Identifier.Trim().ToLower();

        var existUser = await _repositories.UserRepository.GetUserByIdentifierAsync(email: identifier, username: identifier);

        if (existUser is null || !PasswordHasher.VerifyPassword(hashedPassword: existUser.PasswordHash, providedPassword: user.Password))
            throw new InvalidDataAppException("Credentials");

        var strategy = GetTokenStrategy();
        return await AuthenticateUser(strategy, existUser);
    }

    public async Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user)
    {
        await _validationService.ValidateAsync<UserRegisterDto>(user);

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

        await _repositories.UserRepository.AddAsync(userEntity);

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
            invite.UpdatedAt = DateTime.UtcNow;

            await _repositories.OrganizationMemberRepository.AddAsync(membershipEntity);
        }

        await _repositories.UserStatusRepository.AddAsync(userStatusEntity);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var strategy = GetTokenStrategy();
        return await AuthenticateUser(strategy, userEntity);
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

    public async Task LogoutAllUserSessions(Guid? userId = null)
    {
        var strategy = GetTokenStrategy();

        userId = userId ?? Guid.Parse(_httpContext!.GetUserId());

        await RevokeAllActiveSessionsAsync((Guid)userId);
        await _repositories.UnitOfWork.SaveChangesAsync();

        strategy.ClearTokens();
    }

    #endregion

    #region Forgot / Reset Password

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        await _validationService.ValidateAsync<ForgotPasswordDto>(dto);

        var email = dto.Email.Trim().ToLower();

        var user = await _repositories.UserRepository.GetUserByEmailAsync(email: email);
        if (user is null) return;

        var plainOtp = await GenerateAndSaveOtpAsync(userId: user.Id, OtpPurposeEnum.PasswordReset);
        if (string.IsNullOrEmpty(plainOtp)) return;

        await _repositories.UnitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendForgotPasswordOtpAsync(
            toEmail: user.Email,
            userName: user.Name,
            otp: plainOtp,
            expiryMinutes: _tokenSettings.OtpExpirationMinutes);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        await _validationService.ValidateAsync<ResetPasswordDto>(dto);

        var email = dto.Email.Trim().ToLower();

        var user = await _repositories.UserRepository.FindByCondition(x => x.Email == email).FirstOrDefaultAsync();

        if (user is null)
            throw new InvalidDataAppException("Invalid or expired OTP.");

        var otpHash = _tokenService.HashToken(dto.Otp.Trim());

        var otpEntity = await _repositories.OtpVerificationRepository
            .GetActiveOtp(user.Id, OtpPurposeEnum.PasswordReset)
            .FirstOrDefaultAsync();

        if (otpEntity is null || otpEntity.OtpHash != otpHash)
            throw new InvalidDataAppException("Invalid or expired OTP.");

        otpEntity.IsUsed = true;
        otpEntity.UsedAt = DateTime.UtcNow;

        if (PasswordHasher.VerifyPassword(hashedPassword: user.PasswordHash, providedPassword: dto.NewPassword))
        {
            await _repositories.UnitOfWork.SaveChangesAsync();
            throw new InvalidDataAppException("New password must be different from your current password.");
        }

        user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);

        await RevokeAllActiveSessionsAsync(user.Id);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var deviceInfo = _httpContext!.GetDeviceInfo();

        await _emailNotificationService.SendPasswordChangedAlertAsync(
            toEmail: user.Email,
            userName: user.Name,
            device: $"{deviceInfo.Device} {deviceInfo.Browser} {deviceInfo.OS}",
            location: _httpContext!.GetClientIp() //TODO: find the actual location from the IP
        );
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

    public async Task<T> GetUserProfileAsync<T>(Guid? userId = null) where T : class
    {
        userId = userId ?? Guid.Parse(_httpContext!.GetUserId());

        var user = await _repositories.UserRepository.GetById((Guid)userId)
            .ProjectTo<T>(_mapper.ConfigurationProvider)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        return user;
    }

    public async Task UpdateUserAsync(UserUpdateDto model)
    {
        await _validationService.ValidateAsync<UserUpdateDto>(model);

        var userId = Guid.Parse(_httpContext!.GetUserId());

        var user = await _repositories.UserRepository.GetById(userId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        user.Name = model.Name;
        user.Bio = model.Bio;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAvatarAsync(UpdateAvatarDto dto)
    {
        await _validationService.ValidateAsync<UpdateAvatarDto>(dto);

        var userId = Guid.Parse(_httpContext!.GetUserId());

        var fileType = dto.AvatarFile.ValidateFile(FileUsageContextEnum.Avatar);

        var user = await _repositories.UserRepository.GetById(userId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        if (user.AvatarFileId != null)
        {
            var userAvatarFile = await _repositories.FileRepository.GetByIdAsync((Guid)user.AvatarFileId).FirstOrDefaultAsync();

            if (userAvatarFile == null)
                throw new NotFoundAppException("Exist User Avatar file data");

            userAvatarFile.IsDeleted = true;
        }

        var publicId = CloudinaryPublicId.UserAvatar(user.Id);

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.AvatarFile, CloudinaryPath.Users().Avatars(), publicId);

        user.AvatarFile = new FileEntity()
        {
            UploadedByUserId = user.Id,
            UserId = user.Id,
            UsageContext = FileUsageContextEnum.Avatar,

            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            ThumbnailUrl = uploadResult.ThumbnailUrl,

            SizeInBytes = dto.AvatarFile.Length,
            OriginalName = dto.AvatarFile.FileName,
            MimeType = dto.AvatarFile.ContentType,
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

    private async Task RevokeAllActiveSessionsAsync(Guid userId)
    {
        var tokens = await _repositories.RefreshTokenRepository
            .GetActiveRefreshTokensByUserId(userId)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<string?> GenerateAndSaveOtpAsync(Guid userId, OtpPurposeEnum purpose)
    {
        // 1. Check Cooldown
        var latestOtp = await _repositories.OtpVerificationRepository
            .GetLatestOtp(userId, purpose)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (latestOtp is not null)
        {
            var cooldownEndsAt = latestOtp.CreatedAt.AddSeconds(_tokenSettings.OtpResendCooldownSeconds);
            if (DateTime.UtcNow < cooldownEndsAt)
                return null;
        }

        // 2. Invalidate existing active OTPs
        var existingOtps = await _repositories.OtpVerificationRepository
            .GetAllActiveOtps(userId, purpose)
            .ToListAsync();

        foreach (var old in existingOtps)
        {
            old.IsUsed = true;
            old.UsedAt = DateTime.UtcNow;
        }

        // 3. Generate New OTP
        var plainOtp = _tokenService.GenerateOtp();
        var otpEntity = new OtpVerification
        {
            UserId = userId,
            OtpHash = _tokenService.HashToken(plainOtp),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenSettings.OtpExpirationMinutes),
            IPAddress = _httpContext?.GetClientIp(),
            CreatedAt = DateTime.UtcNow,
        };

        await _repositories.OtpVerificationRepository.AddAsync(otpEntity);
        return plainOtp;
    }

    private IAuthTokenStrategy GetTokenStrategy() => _tokenStrategyFactory.Resolve();

    #endregion
}
