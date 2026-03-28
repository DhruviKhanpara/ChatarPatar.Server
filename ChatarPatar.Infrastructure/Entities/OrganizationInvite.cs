using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class OrganizationInvite : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(CreatedBy))]
    public User CreatedByUser { get; set; } = null!;

    [ForeignKey(nameof(UsedBy))]
    public User? UsedByUser { get; set; }
    #endregion

    public Guid OrganizationId { get; set; }
    public Guid CreatedBy { get; set; }

    public string Email { get; set; } = null!;
    public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.OrgMember;
    public string Token { get; set; } = null!;

    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? UsedBy { get; set; }

    public DateTime ExpiresAt { get; set; } // typically CreatedAt + 7 days
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
