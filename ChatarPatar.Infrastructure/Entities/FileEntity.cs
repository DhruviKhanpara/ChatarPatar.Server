using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class FileEntity : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(OrgId))]
    public Organization? Organization { get; set; }
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }
    [ForeignKey(nameof(UploadedByUserId))]
    public User? UploadedByUser { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    #endregion

    public Guid UploadedByUserId { get; set; }

    // Cloudinary fields
    public string PublicId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }

    public FileTypeEnum FileType { get; set; }
    public FileUsageContextEnum UsageContext { get; set; }
    public string MimeType { get; set; } = null!;
    public long SizeInBytes { get; set; }
    public string OriginalName { get; set; } = null!;

    // Ownership scope
    public Guid? UserId { get; set; }
    public Guid? OrgId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
}