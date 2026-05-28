using ChatarPatar.Application.DTOs.File;

namespace ChatarPatar.Application.ServiceContracts;

public interface IFileService
{
    /// <summary>
    /// Uploads a file to Cloudinary as a pending attachment and creates a
    /// Files row with Status=Pending and no scope.
    /// </summary>
    Task<UploadAttachmentResponseDto> UploadAttachmentAsync(UploadAttachmentDto dto);

    /// <summary>
    /// Flips one or more pending attachment rows to Attached, sets their scope
    /// (ChannelId or ConversationId), and clears ExpiresAt.
    /// Called inside SendMessageAsync's transaction — not exposed as an HTTP endpoint.
    /// </summary>
    Task AttachFilesToMessageAsync(List<Guid> fileIds, Guid uploadedByUserId, Guid? channelId, Guid? conversationId);

    /// <summary>
    /// Cleanup job entry point: deletes expired pending attachment rows from
    /// the DB and their corresponding Cloudinary assets.
    /// Designed to be called by a hosted background service on a schedule.
    /// </summary>
    Task DeleteExpiredPendingAttachmentsAsync();
}
