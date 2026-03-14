using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Team : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(OrgId))]
    public Organization Organization { get; set; } = null!;
    [ForeignKey(nameof(IconFileId))]
    public Files? IconFile { get; set; }
    [ForeignKey(nameof(ArchivedBy))]
    public User? ArchivedByUser { get; set; }

    public virtual List<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public virtual List<Channel> Channels { get; set; } = new List<Channel>();
    #endregion

    public Guid OrgId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Guid? IconFileId { get; set; }
    
    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }
    public Guid? ArchivedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}