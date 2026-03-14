using AutoMapper;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.RepositoryContracts;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Security;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ChatarPatar.Application.Services;

internal class UserService : IUserService
{
    private const string ACCESS_TOKEN_COOKIE = "AccessToken";
    private const string REFRESH_TOKEN_COOKIE = "RefreshToken";
    private readonly double _tokenExpirationMinutes;
    private readonly double _refreshTokenExpirationDay;
    private readonly int _maxSessions;

    private readonly IRepositoryManager _repositories;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenService _tokenService;
    private readonly IMapper _mapper;

    public UserService(IRepositoryManager repositories, IExternalServiceManager externalServiceManager, IValidationService validationService, IHttpContextAccessor httpContextAccessor, TokenService tokenService, IMapper mapper, IConfiguration config)
    {
        _repositories = repositories;
        _externalServiceManager = externalServiceManager;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _mapper = mapper;
        _tokenExpirationMinutes = config.GetValue<double>("AppSettings:TokenExpirationMinutes");
        _refreshTokenExpirationDay = config.GetValue<double>("AppSettings:RefreshTokenExpirationDays");
        _maxSessions = config.GetValue<int>("AppSettings:MaxSessions");
    }

    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    public async Task<LoginResponseDto> LoginUserAsync(UserLoginDto user)
    {
        _validationService.Validate<UserLoginDto>(user);

        var identifier = user.Identifier.Trim().ToLower();

        var existUser = await _repositories.UserRepository
            .FindByCondition(x => (x.Email.ToLower() == identifier || x.Username.ToLower() == identifier))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (existUser is null)
            throw new InvalidDataAppException("Credentials");

        if (!PasswordHasher.VerifyPassword(hashedPassword: existUser.PasswordHash, providedPassword: user.Password))
            throw new InvalidDataAppException("Credentials");

        return await AuthenticateUser(existUser);
    }

    public async Task RefreshAuthToken()
    {
        var refreshToken = _httpContext?.Request.Cookies[REFRESH_TOKEN_COOKIE];

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidDataAppException("");

        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _repositories.RefreshTokenRepository.FindByCondition(x => x.Token == tokenHash && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync();

        if (storedToken == null)
            throw new NotFoundAppException("Invalid refresh token");

        var user = await _repositories.UserRepository.GetByIdAsync(storedToken.UserId).FirstOrDefaultAsync();

        if (user == null)
            throw new InvalidDataAppException("User");

        await AuthenticateUser(user, storedToken);
    }

    public async Task LogoutUser()
    {
        var refreshToken = _httpContext?.Request.Cookies[REFRESH_TOKEN_COOKIE];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var storedToken = await _repositories.RefreshTokenRepository.FindByCondition(x => x.Token == tokenHash).FirstOrDefaultAsync();

            if (storedToken != null)
            {
                RevokeToken(storedToken);
                await _repositories.UnitOfWork.SaveChangesAsync();
            }
        }

        _httpContext?.Response.Cookies.Delete(ACCESS_TOKEN_COOKIE, new CookieOptions { Path = "/" });
        _httpContext?.Response.Cookies.Delete(REFRESH_TOKEN_COOKIE, new CookieOptions { Path = "/" });
    }

    public async Task RevokeAllUserSessions(Guid? userId)
    {
        userId = userId ?? Guid.Parse(_httpContext!.GetUserId());

        var tokens = await _repositories.RefreshTokenRepository.FindByCondition(x => x.UserId == userId && !x.IsRevoked).ToListAsync();

        foreach (var token in tokens)
        {
            RevokeToken(token);
        }
        await _repositories.UnitOfWork.SaveChangesAsync();

        _httpContext?.Response.Cookies.Delete(ACCESS_TOKEN_COOKIE, new CookieOptions { Path = "/" });
        _httpContext?.Response.Cookies.Delete(REFRESH_TOKEN_COOKIE, new CookieOptions { Path = "/" });
    }

    public async Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user)
    {
        _validationService.Validate<UserRegisterDto>(user);

        var username = user.Username.Trim().ToLower();
        var email = user.Email.Trim().ToLower();

        var existingUser = await _repositories.UserRepository
            .FindByCondition(x => x.Username.ToLower() == username || x.Email.ToLower() == email)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            if (existingUser.Username.ToLower() == username)
                throw new InvalidDataAppException("Username is already registered");

            if (existingUser.Email.ToLower() == email)
                throw new InvalidDataAppException("Email is already registered");
        }

        var userEntity = _mapper.Map<User>(user);
        userEntity.PasswordHash = PasswordHasher.HashPassword(user.Password);

        await _repositories.UserRepository.AddAsync(entity: userEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();

        return await AuthenticateUser(userEntity);
    }

    public async Task UpdateAvatarAsync(Guid userId, IFormFile file)
    {
        var fileType = file.ValidateFile(FileUsageContextEnum.Avatar);

        var user = await _repositories.UserRepository.GetByIdAsync(userId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(file, FileFolderEnum.Avatar.BuildFolder(), FileFolderEnum.Avatar.BuildPublicId(user.Id));

        user.AvatarFile = new Files()
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

    #region Private section

    private async Task<LoginResponseDto> AuthenticateUser(User user, RefreshToken? entity = null)
    {
        string token = _tokenService.CreateToken(email: user.Email, id: user.Id, name: user.Name);

        _httpContext?.Response.Cookies.Append(ACCESS_TOKEN_COOKIE, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Expires = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes)
        });

        await IssueRefreshTokenAsync(user, entity);

        return new LoginResponseDto()
        {
            AccessToken = token,
            ExpiredIn = _tokenExpirationMinutes * 60,
            TokenType = "Bearer"
        };
    }

    private async Task IssueRefreshTokenAsync(User user, RefreshToken? entity = null)
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
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDay)
        };

        if (entity != null)
        {
            refreshTokenEntity.Id = entity.Id;
            refreshTokenEntity.UpdatedAt = DateTime.UtcNow;

            _repositories.RefreshTokenRepository.Update(entity, refreshTokenEntity);
        }
        else
        {
            var tokensToRevoke = await _repositories.RefreshTokenRepository
            .FindByCondition(x => x.UserId == user.Id && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(_maxSessions - 1)
            .ToListAsync();

            foreach (var token in tokensToRevoke)
            {
                RevokeToken(token);
            }

            await _repositories.RefreshTokenRepository.AddAsync(refreshTokenEntity);
        }

        await _repositories.UnitOfWork.SaveChangesAsync();

        _httpContext?.Response.Cookies.Append(REFRESH_TOKEN_COOKIE, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(_refreshTokenExpirationDay)
        });
    }

    private void RevokeToken(RefreshToken token)
    {
        var tokenEntity = new RefreshToken()
        {
            Id = token.Id,
            UserId = token.UserId,
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _repositories.RefreshTokenRepository.Update(token, tokenEntity);
    }

    #endregion
}
