using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class UserService : IUserService
{
    private readonly IRepositoryManager _repositories;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<UserService> _logger;

    public UserService(IRepositoryManager repositories, IExternalServiceManager externalServiceManager, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IMapper mapper, IEmailNotificationService emailNotificationService, ILogger<UserService> logger)
    {
        _repositories = repositories;
        _externalServiceManager = externalServiceManager;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<AuthUserDto> GetCurrentUserAsync()
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository.GetById(userId)
            .AsNoTracking()
            .ProjectTo<AuthUserDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("Login User data");

        return user;
    }

    public async Task<T> GetUserProfileAsync<T>(Guid? userId = null) where T : class
    {
        userId = userId ?? Guid.Parse(_httpContext.GetUserId());

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

        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository.GetById(userId).FirstOrDefaultAsync();

        if (user == null)
            throw new NotFoundAppException("User");

        _mapper.Map<UserUpdateDto, User>(model, user);
        user.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateUserAvatarAsync(ImageUploadDto dto)
    {
        await _validationService.ValidateAsync<ImageUploadDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());
        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Avatar);

        var user = await _repositories.UserRepository.GetById(userId).FirstOrDefaultAsync();
        if (user == null)
            throw new NotFoundAppException("User");

        FileUploadResult? uploadResult = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (user.AvatarFileId != null)
            {
                var oldFile = await _repositories.FileRepository.GetByIdAsync(user.AvatarFileId.Value).FirstOrDefaultAsync();

                if (oldFile != null)
                    oldFile.IsDeleted = true;
            }

            var publicId = CloudinaryPublicId.UserAvatar(user.Id);
            uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Users().Avatars(), publicId);

            user.AvatarFile = new FileEntity
            {
                UploadedByUserId = user.Id,
                UserId = user.Id,
                UsageContext = FileUsageContextEnum.Avatar,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url,
                ThumbnailUrl = uploadResult.ThumbnailUrl,
                SizeInBytes = dto.File.Length,
                OriginalName = dto.File.FileName,
                MimeType = dto.File.ContentType,
                FileType = fileType,
            };

            user.UpdatedAt = DateTime.UtcNow;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();

            if (uploadResult != null)
            {
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete user avatar from Cloudinary. PublicId: {PublicId}", uploadResult.PublicId);
                }
            }

            throw;
        }
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        await _validationService.ValidateAsync<ChangePasswordDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository
            .GetById(userId)
            .FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        var isValid = PasswordHasher.VerifyPassword(hashedPassword: user.PasswordHash, providedPassword: dto.CurrentPassword);
        if (!isValid)
            throw new UnauthorizedAppException("Current password is incorrect", ExceptionCodes.INVALID_CREDENTIALS);

        if (PasswordHasher.VerifyPassword(hashedPassword: user.PasswordHash, providedPassword: dto.NewPassword))
            throw new InvalidDataAppException("New password must be different from your current password.", ExceptionCodes.PASSWORD_SAME_AS_CURRENT);

        user.PasswordHash = PasswordHasher.HashPassword(password: dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // revoke all sessions except current
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

    public async Task RemoveUserAvatarAsync()
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var user = await _repositories.UserRepository
            .GetById(userId)
            .Include(x => x.AvatarFile)
            .FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        if (user.AvatarFileId == null)
            return;

        string? oldPublicId = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (user.AvatarFile != null)
            {
                oldPublicId = user.AvatarFile.PublicId;
                user.AvatarFile.IsDeleted = true;
            }

            user.AvatarFileId = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (oldPublicId != null)
        {
            try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(oldPublicId); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete user avatar from Cloudinary. PublicId: {PublicId}", oldPublicId);
            }
        }
    }

    #region Private section

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

    #endregion
}
