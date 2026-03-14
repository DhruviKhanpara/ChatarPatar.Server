using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class OrganizationMember : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(OrgId))]
    public Organization Organization { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(InvitedByUserId))]
    public User? InvitedByUser { get; set; }
    #endregion

    public Guid OrgId { get; set; }
    public Guid UserId { get; set; }

    public OrganizationRoleEnum Role { get; set; }

    public Guid? InvitedByUserId { get; set; }

    public DateTime JoinedAt { get; set; }
}