using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class MessageReceipt : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    #endregion

    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }

    public DateTime? DeliveredAt { get; set; }
    public DateTime? SeenAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}