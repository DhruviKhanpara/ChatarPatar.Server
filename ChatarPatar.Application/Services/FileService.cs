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

    public async Task<UploadAttachmentResponseDto> UploadAttachmentAsync(UploadAttachmentDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());
        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Attachment);

        var folder = dto.ChannelId.HasValue
            ? AttachmentFolderResolver.ForChannel(dto.OrgId!.Value, dto.TeamId!.Value, dto.ChannelId.Value, fileType)
            : AttachmentFolderResolver.ForConversation(dto.ConversationId!.Value, fileType);

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
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId); }
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

    public async Task AttachFilesToMessageAsync(List<Guid> fileIds, Guid uploadedByUserId, Guid? channelId, Guid? conversationId)
    {
        if (fileIds.Count == 0) return;

        if (channelId.HasValue == conversationId.HasValue)
            throw new AppException("Exactly one of channelId or conversationId must be provided.");

        var files = await _repositories.FileRepository
            .GetPendingAttachmentsByIdsAsync(fileIds, uploadedByUserId);

        if (files.Count != fileIds.Count)
        {
            var missingIds = fileIds.Except(files.Select(f => f.Id)).ToList();
            throw new InvalidDataAppException(
                $"One or more files could not be attached. " +
                $"They may not exist, may already be attached, or may belong to another user. " +
                $"Missing: {string.Join(", ", missingIds)}");
        }

        foreach (var file in files)
        {
            file.Status = FileStatusEnum.Attached;
            file.ExpiresAt = null;
            file.ChannelId = channelId;
            file.ConversationId = conversationId;
        }
        // Caller (SendMessageAsync) saves within its own transaction — no SaveChanges here.
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

        foreach (var file in expired)
        {
            try
            {
                await _externalServiceManager.CloudinaryService.DeleteFileAsync(file.PublicId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup: failed to delete Cloudinary asset {PublicId} for file {FileId}. Will retry next run.", file.PublicId, file.Id);
                continue;
            }

            file.IsDeleted = true;
        }

        await _repositories.UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cleanup: expired pending attachments deleted.");
    }
}
