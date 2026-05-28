using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.DTOs.File;

/// <summary>
/// Multipart/form-data payload for the attachment pre-upload endpoint.
/// </summary>
public sealed class UploadAttachmentDto
{
    public IFormFile File { get; set; } = null!;

    // Scope context — exactly one of these pairs must be filled:

    // Channel upload: all three required together
    public Guid? OrgId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ChannelId { get; set; }

    // Conversation upload
    public Guid? ConversationId { get; set; }
}