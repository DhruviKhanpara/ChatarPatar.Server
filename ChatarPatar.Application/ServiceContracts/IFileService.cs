using ChatarPatar.Application.DTOs.File;

namespace ChatarPatar.Application.ServiceContracts;

public interface IFileService
{
    /// <summary>
    /// Uploads a Channel attachment file to Cloudinary as a pending attachment and creates a
    /// Files row with Status=Pending and no scope.
    /// </summary>
    Task<UploadAttachmentResponseDto> UploadChannelAttachmentAsync(Guid orgId, Guid teamId, Guid channelId, UploadAttachmentDto dto);

    /// <summary>
    /// Uploads a Conversation attachment file to Cloudinary as a pending attachment and creates a
    /// Files row with Status=Pending and no scope.
    /// </summary>
    Task<UploadAttachmentResponseDto> UploadConversationAttachmentAsync(Guid conversationId, UploadAttachmentDto dto);

    /// <summary>
    /// Cleanup job entry point: deletes expired pending attachment rows from
    /// the DB and their corresponding Cloudinary assets.
    /// Designed to be called by a hosted background service on a schedule.
    /// </summary>
    Task DeleteExpiredPendingAttachmentsAsync();
}
