using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Message : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    [ForeignKey(nameof(SenderId))]
    public User Sender { get; set; } = null!;
    [ForeignKey(nameof(ThreadRootMessageId))]
    public Message? Thread { get; set; }
    [ForeignKey(nameof(DeletedBy))]
    public User? DeletedByUser { get; set; }
    #endregion

    public long SequenceNumber { get; set; }
    public Guid ClientMessageId { get; set; }

    // Source (exactly one must be set)
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid SenderId { get; set; }

    // Threading
    public Guid? ThreadRootMessageId { get; set; }

    // Content
    public string? Content { get; set; }
    public MessageTypeEnum MessageType { get; set; }

    // Edit tracking
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }

    // Thread summary (root only)
    public int ReplyCount { get; set; }
    public DateTime? LastReplyAt { get; set; }

    // 1-on-1 DM state
    public DmMessageStatusEnum? DmStatus { get; set; }
    public DateTime? DmDeliveredAt { get; set; }
    public DateTime? DmSeenAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}