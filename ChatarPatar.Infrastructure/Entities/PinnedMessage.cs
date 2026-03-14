using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class PinnedMessage : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    public Channel? Channel { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
    [ForeignKey(nameof(PinnedByUserId))]
    public User PinnedByUser { get; set; } = null!;
    [ForeignKey(nameof(UnPinnedByUserId))]
    public User? UnPinnedByUser { get; set; }
    #endregion

    public Guid MessageId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
    

    public Guid PinnedByUserId { get; set; }
    public DateTime PinnedAt { get; set; }

    public Guid? UnPinnedByUserId { get; set; }
    public DateTime? UnPinnedAt { get; set; }

    public string? ContentSnapshot { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}