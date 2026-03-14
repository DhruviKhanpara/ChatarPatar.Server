using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class MessageMention : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    #endregion

    public Guid MessageId { get; set; }

    public Guid MentionedUserId { get; set; }
    public User MentionedUser { get; set; } = null!;

    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }

    public DateTime CreatedAt { get; set; }
}