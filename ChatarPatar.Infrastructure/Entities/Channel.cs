using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Channel : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(TeamId))]
    public Team Team { get; set; } = null!;
    [ForeignKey(nameof(OrgId))]
    public Organization Organization { get; set; } = null!;
    [ForeignKey(nameof(ArchivedBy))]
    public User? ArchivedByUser { get; set; }

    public virtual List<ChannelMember> ChannelMembers { get; set; } = new List<ChannelMember>();
    #endregion

    public Guid TeamId { get; set; }
    public Guid OrgId { get; set; }
    
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ChannelTypeEnum Type { get; set; }

    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }
    public Guid? ArchivedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}