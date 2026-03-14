using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class MessageReaction : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    #endregion

    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }

    public string Emoji { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}