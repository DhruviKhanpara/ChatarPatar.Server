using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Message;

public sealed class MessageDto
{
    public Guid Id { get; set; }
    public long SequenceNumber { get; set; }
    public Guid ClientMessageId { get; set; }

    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }

    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string? SenderAvatarThumbnailUrl { get; set; }

    public Guid? ThreadRootMessageId { get; set; }

    public string? Content { get; set; }
    public MessageTypeEnum MessageType { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }

    public int ReplyCount { get; set; }
    public DateTime? LastReplyAt { get; set; }
    public DateTime? DmSeenAt { get; set; }

    // DM-only; null for channel messages
    public DmMessageStatusEnum? DmStatus { get; set; }
    public DateTime? DmDeliveredAt { get; set; }

    public List<MessageAttachmentDto> Attachments { get; set; } = [];
    public List<MessageMentionDto> Mentions { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
