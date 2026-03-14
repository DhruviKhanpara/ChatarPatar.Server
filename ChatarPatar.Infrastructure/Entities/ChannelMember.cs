using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class ChannelMember : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(ChannelId))]
    public Channel Channel { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
    #endregion

    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }

    public ChannelRoleEnum Role { get; set; }

    public Guid? AddedByUserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public bool IsMuted { get; set; }
}