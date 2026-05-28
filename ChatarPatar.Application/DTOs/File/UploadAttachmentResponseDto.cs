namespace ChatarPatar.Application.DTOs.File;

/// <summary>
/// Returned immediately after a successful pre-upload.
/// The client holds this FileId and includes it in SendMessageDto.FileIds
/// when the user sends the message.
/// </summary>
public sealed class UploadAttachmentResponseDto
{
    /// <summary>
    /// The FileId to pass in SendMessageDto.FileIds.
    /// </summary>
    public Guid FileId { get; set; }

    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }

    public string OriginalName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Derived file category — lets the client render the right preview
    /// (image thumbnail, video player, document icon, etc.).
    /// </summary>
    public string FileType { get; set; } = null!;

    /// <summary>
    /// Clients can use this to show a "file will expire" warning if the user is idle for a long time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}