using ChatarPatar.Application.DTOs.File;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class FileService : IFileService
{
    private static readonly TimeSpan pendingTtl = TimeSpan.FromHours(24);

    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FileService> _logger;

    public FileService(IRepositoryManager repositories, IValidationService validationService, IExternalServiceManager externalServiceManager, IHttpContextAccessor httpContextAccessor, ILogger<FileService> logger)
    {
        _repositories = repositories;
        _validationService = validationService;
        _externalServiceManager = externalServiceManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<UploadAttachmentResponseDto> UploadChannelAttachmentAsync(Guid orgId, Guid teamId, Guid channelId, UploadAttachmentDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Attachment);

        var folder = AttachmentFolderResolver.ForChannel(orgId, teamId, channelId, fileType);

        return await UploadAttachmentAsync(dto, folder, fileType);
    }

    public async Task<UploadAttachmentResponseDto> UploadConversationAttachmentAsync(Guid conversationId, UploadAttachmentDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Attachment);

        var folder = AttachmentFolderResolver.ForConversation(conversationId, fileType);

        return await UploadAttachmentAsync(dto, folder, fileType);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  CLEANUP JOB
    // ──────────────────────────────────────────────────────────────────────

    public async Task DeleteExpiredPendingAttachmentsAsync()
    {
        var expired = await _repositories.FileRepository
            .GetExpiredPendingAttachmentsQuery()
            .ToListAsync();

        if (expired.Count == 0) return;

        _logger.LogInformation("Cleanup: found {Count} expired pending attachments.", expired.Count);

        int deletedCount = 0;
        int failedCount = 0;
        foreach (var file in expired)
        {
            try
            {
                await _externalServiceManager.CloudinaryService.DeleteFileAsync(file.PublicId, file.FileType);
                _repositories.FileRepository.Remove(file);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup: failed to delete Cloudinary asset {PublicId} for file {FileId}. Will retry next run.", file.PublicId, file.Id);
                failedCount++;
            }
        }

        await _repositories.UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cleanup: deleted {DeletedCount} expired pending attachments. {FailedCount} failed and will retry later.", deletedCount, failedCount);
    }

    #region Private Section

    private async Task<UploadAttachmentResponseDto> UploadAttachmentAsync(UploadAttachmentDto dto, string folder, FileTypeEnum fileType)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadAttachmentAsync(dto.File, folder, fileType);

        var expiresAt = DateTime.UtcNow.Add(pendingTtl);

        var fileEntity = new FileEntity
        {
            UploadedByUserId = authUserId,
            UsageContext = FileUsageContextEnum.Attachment,

            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            ThumbnailUrl = uploadResult.ThumbnailUrl,

            SizeInBytes = dto.File.Length,
            OriginalName = dto.File.FileName,
            MimeType = dto.File.ContentType,
            FileType = fileType,

            // Pending: no scope yet, expiry set
            Status = FileStatusEnum.Pending,
            ExpiresAt = expiresAt,
        };

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.FileRepository.AddAsync(fileEntity);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            // DB insert failed — remove the Cloudinary asset immediately
            await tx.RollbackAsync();

            if (uploadResult != null)
            {
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId, fileType); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete Cloudinary asset after DB insert failure. PublicId: {PublicId}", uploadResult.PublicId);
                }
            }

            throw;
        }

        return new UploadAttachmentResponseDto
        {
            FileId = fileEntity.Id,
            Url = fileEntity.Url,
            ThumbnailUrl = fileEntity.ThumbnailUrl,
            OriginalName = fileEntity.OriginalName,
            MimeType = fileEntity.MimeType,
            SizeInBytes = fileEntity.SizeInBytes,
            FileType = fileEntity.FileType.ToString(),
            ExpiresAt = expiresAt,
        };
    }

    #endregion
}
