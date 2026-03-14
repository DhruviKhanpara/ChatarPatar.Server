using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class ReadState : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    #endregion

    public Guid UserId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }

    public int UnreadCount { get; set; }
    public int MentionCount { get; set; }

    public long LastReadSequenceNumber { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public Message? LastReadMessage { get; set; }

    public DateTime? LastReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}