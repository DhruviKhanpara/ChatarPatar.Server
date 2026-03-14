using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Organization : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(LogoFileId))]
    public Files? LogoFile { get; set; }

    public virtual List<OrganizationMember> OrganizationMembers { get; set; } = new List<OrganizationMember>();
    public virtual List<Team> Teams { get; set; } = new List<Team>();
    #endregion

    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public Guid? LogoFileId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}