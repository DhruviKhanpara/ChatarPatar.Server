using AutoMapper;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
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
using System.Net.Http;

namespace ChatarPatar.Application.Services;

internal class AuthService : IAuthService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;
    private readonly AuthTokenStrategyFactory _tokenStrategyFactory;
    private readonly IMapper _mapper;
    private readonly TokenSettings _tokenSettings;
    private readonly IEmailNotificationService _emailNotificationService;

    public AuthService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IMapper mapper, AuthTokenStrategyFactory tokenStrategyFactory, IOptions<TokenSettings> tokenSettings, IEmailNotificationService emailNotificationService)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _mapper = mapper;
        _tokenStrategyFactory = tokenStrategyFactory;
        _tokenSettings = tokenSettings.Value;
        _emailNotificationService = emailNotificationService;
    }

    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<LoginResponseDto> LoginUserAsync(UserLoginDto user)
    {
        await _validationService.ValidateAsync<UserLoginDto>(user);

        var identifier = user.Identifier.Trim().ToLower();

        var existUser = await _repositories.UserRepository.GetUserByIdentifierAsync(email: identifier, username: identifier);

        if (existUser is null || !PasswordHasher.VerifyPassword(hashedPassword: existUser.PasswordHash, providedPassword: user.Password))
            throw new UnauthorizedAppException("Invalid Credentials", ExceptionCodes.INVALID_CREDENTIALS);

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

            // ── BRUTE-FORCE / ENUMERATION PROTECTION ──────────────────────────
            if (invite is null)
            {
                var inviteByEmail = await _repositories.OrganizationInviteRepository
                    .GetPendingByEmail(email)
                    .FirstOrDefaultAsync();

                if (inviteByEmail is not null)
                {
                    inviteByEmail.FailedAttempts++;
                    inviteByEmail.UpdatedAt = DateTime.UtcNow;

                    if (inviteByEmail.FailedAttempts >= _tokenSettings.InviteMaxFailedAttempts)
                    {
                        // Void the invite — the sender will need to issue a new one
                        inviteByEmail.IsUsed = true;
                        inviteByEmail.UsedAt = DateTime.UtcNow;
                        await _repositories.UnitOfWork.SaveChangesAsync();
                        throw new InvalidDataAppException("Invite token is invalid or has expired");
                    }

                    await _repositories.UnitOfWork.SaveChangesAsync();
                }

                throw new InvalidDataAppException("Invite token is invalid or has expired");
            }

            if (invite.Email != email)
                throw new ForbiddenAppException("The Invite was sent to other email.");

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

        // NOTE: The user, membership, and invite.IsUsed=true are all written in a single
        // SaveChangesAsync call. If it fails, EF rolls back the entire unit of work —
        // the invite is NOT consumed and the user can retry with the same token.
        await _repositories.UnitOfWork.SaveChangesAsync();

        // ── SEND VERIFICATION OTP ──────────────────────────────────────────────
        // Runs after the user row is persisted so the OTP record can reference UserId.
        // Email dispatch is fire-and-forget — a delivery failure must not block the
        // registration response. The user can always resend via /resend-verification.
        var plainOtp = await GenerateAndSaveOtpAsync(userEntity.Id, OtpPurposeEnum.EmailVerification);
        if (plainOtp is not null)
        {
            await _repositories.UnitOfWork.SaveChangesAsync();
            _ = _emailNotificationService.SendEmailVerificationOtpAsync(
                toEmail: userEntity.Email,
                userName: userEntity.Name,
                otp: plainOtp,
                expiryMinutes: _tokenSettings.OtpExpirationMinutes);
        }

        var strategy = GetTokenStrategy();
        return await AuthenticateUser(strategy, userEntity);
    }

    public async Task<LoginResponseDto> RefreshAuthToken()
    {
        var strategy = GetTokenStrategy();
        var refreshToken = strategy.GetRefreshToken();

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAppException("Invalid refresh token", ExceptionCodes.REFRESH_TOKEN_INVALID);

        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _repositories.RefreshTokenRepository.FindActiveRefreshToken(token: tokenHash).AsNoTracking().FirstOrDefaultAsync();

        if (storedToken == null)
            throw new UnauthorizedAppException("Invalid refresh token", ExceptionCodes.REFRESH_TOKEN_INVALID);

        var user = await _repositories.UserRepository.GetById(storedToken.UserId).FirstOrDefaultAsync();

        if (user == null)
            throw new UnauthorizedAppException("Invalid refresh token", ExceptionCodes.REFRESH_TOKEN_INVALID);

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

        userId = userId ?? Guid.Parse(_httpContext.GetUserId());

        await RevokeAllActiveSessionsAsync((Guid)userId);
        await _repositories.UnitOfWork.SaveChangesAsync();

        strategy.ClearTokens();
    }

    #region Email Verification

    public async Task VerifyEmailAsync(VerifyEmailDto dto)
    {
        await _validationService.ValidateAsync<VerifyEmailDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository
            .GetById(userId)
            .FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        if (user.IsEmailVerified)
            throw new InvalidDataAppException("Email is already verified.");

        var otpEntity = await VerifyOtpAsync(userId: userId, OtpPurposeEnum.EmailVerification, dto.Otp);

        user.IsEmailVerified = true;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task ResendVerificationOtpAsync()
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository
            .GetById(userId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        if (user.IsEmailVerified)
            return;

        var plainOtp = await GenerateAndSaveOtpAsync(userId, OtpPurposeEnum.EmailVerification);
        if (plainOtp is null)
            return;

        await _repositories.UnitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendEmailVerificationOtpAsync(
            toEmail: user.Email,
            userName: user.Name,
            otp: plainOtp,
            expiryMinutes: _tokenSettings.OtpExpirationMinutes);
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

        var otpEntity = await VerifyOtpAsync(userId: user.Id, OtpPurposeEnum.PasswordReset, dto.Otp);

        if (PasswordHasher.VerifyPassword(hashedPassword: user.PasswordHash, providedPassword: dto.NewPassword))
            throw new InvalidDataAppException("New password must be different from your current password.", ExceptionCodes.PASSWORD_SAME_AS_CURRENT);

        user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);

        await RevokeAllActiveSessionsAsync(user.Id);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var deviceInfo = _httpContext.GetDeviceInfo();

        await _emailNotificationService.SendPasswordChangedAlertAsync(
            toEmail: user.Email,
            userName: user.Name,
            device: $"{deviceInfo.Device} {deviceInfo.Browser} {deviceInfo.OS}",
            location: _httpContext.GetClientIp() //TODO: find the actual location from the IP
        );
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
        var deviceInfo = _httpContext.GetDeviceInfo();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.HashToken(refreshToken),
            Device = deviceInfo.Device,
            Browser = deviceInfo.Browser,
            OperatingSystem = deviceInfo.OS,
            IPAddress = _httpContext.GetClientIp(),
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

    private async Task<OtpVerification> VerifyOtpAsync(Guid userId, OtpPurposeEnum purpose, string otp)
    {
        var otpEntity = await _repositories.OtpVerificationRepository
            .GetActiveOtp(userId, purpose)
            .FirstOrDefaultAsync();

        var otpHash = _tokenService.HashToken(otp.Trim());

        if (otpEntity is null)
            throw new InvalidDataAppException("Invalid or expired OTP.");

        // ── BRUTE-FORCE PROTECTION ─────────────────────────────────────────────
        if (otpEntity.OtpHash != otpHash)
        {
            otpEntity.FailedAttempts++;

            if (otpEntity.FailedAttempts >= _tokenSettings.OtpMaxFailedAttempts)
            {
                // Invalidate — force the user to go through forgot-password again
                otpEntity.IsUsed = true;
                otpEntity.UsedAt = DateTime.UtcNow;
                await _repositories.UnitOfWork.SaveChangesAsync();
                throw new InvalidDataAppException("Invalid or expired OTP.");
            }

            await _repositories.UnitOfWork.SaveChangesAsync();
            throw new InvalidDataAppException("Invalid or expired OTP.");
        }

        // ── CORRECT OTP ────────────────────────────────────────────────────────
        otpEntity.IsUsed = true;
        otpEntity.UsedAt = DateTime.UtcNow;

        return otpEntity;
    }

    private IAuthTokenStrategy GetTokenStrategy() => _tokenStrategyFactory.Resolve();

    #endregion
}
