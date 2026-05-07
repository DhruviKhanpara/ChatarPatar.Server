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
    private readonly IMapper _mapper;
    private readonly IEmailNotificationService _emailNotificationService;

    public UserService(IRepositoryManager repositories, IExternalServiceManager externalServiceManager, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IMapper mapper, IEmailNotificationService emailNotificationService)
    {
        _repositories = repositories;
        _externalServiceManager = externalServiceManager;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _emailNotificationService = emailNotificationService;
    }

    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    #region Basic User Operations

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

    public async Task UpdateAvatarAsync(ImageUploadDto dto)
    {
        await _validationService.ValidateAsync<ImageUploadDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Avatar);

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

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Users().Avatars(), publicId);

        user.AvatarFile = new FileEntity()
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

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
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
    #endregion

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
