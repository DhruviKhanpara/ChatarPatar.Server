using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Conversation : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(OrgId))]
    public Organization Organization { get; set; } = null!;
    [ForeignKey(nameof(LogoFileId))]
    public FileEntity? LogoFile { get; set; }

    public virtual List<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
    #endregion

    public Guid OrgId { get; set; }
    public ConversationTypeEnum Type { get; set; }

    public string? Name { get; set; }   // Only for Group
    public Guid? LogoFileId { get; set; }
}