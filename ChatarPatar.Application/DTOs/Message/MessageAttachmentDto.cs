using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Message;

public sealed class MessageAttachmentDto
{
    public Guid FileId { get; set; }
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string OriginalName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long SizeInBytes { get; set; }
    public FileTypeEnum FileType { get; set; }
    public int DisplayOrder { get; set; }
}
