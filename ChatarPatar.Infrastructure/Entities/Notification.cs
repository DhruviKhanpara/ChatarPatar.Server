using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Notification : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message? Message { get; set; }
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    [ForeignKey(nameof(RecipientId))]
    public User Recipient { get; set; } = null!;
    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }
    [ForeignKey(nameof(ActorId))]
    public User? Actor { get; set; }
    #endregion

    public Guid RecipientId { get; set; }
    public NotificationTypeEnum Type { get; set; }

    public Guid? MessageId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? ActorId { get; set; }

    public string? Preview { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}