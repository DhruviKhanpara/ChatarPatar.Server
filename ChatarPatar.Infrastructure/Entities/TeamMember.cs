using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class TeamMember : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(TeamId))]
    public Team Team { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(InvitedByUserId))]
    public User? InvitedByUser { get; set; }
    #endregion

    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }

    public TeamRoleEnum Role { get; set; }

    public Guid? InvitedByUserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public bool IsMuted { get; set; }
}