using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class Conversation : AuditableEntity
{
    #region Table References
    [ForeignKey(nameof(LogoFileId))]
    public FileEntity? LogoFile { get; set; }
    [ForeignKey(nameof(DirectParticipantAId))]
    public User? DirectParticipantA { get; set; }
    [ForeignKey(nameof(DirectParticipantBId))]
    public User? DirectParticipantB { get; set; }

    public virtual List<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
    public virtual List<Message> Messages { get; set; } = new List<Message>();
    public virtual List<ReadState> ReadStates { get; set; } = new List<ReadState>();
    #endregion

    public ConversationTypeEnum Type { get; set; }

    // Only for Group
    public string? Name { get; set; }
    public Guid? LogoFileId { get; set; }

    // Direct DM columns

    /// <summary>
    /// Populated only when Type = Direct.
    /// Always the GUID that compares smaller (Min of the two)
    /// </summary>
    public Guid? DirectParticipantAId { get; set; }

    /// <summary>
    /// Populated only when Type = Direct.
    /// Always the GUID that compares larger (Max of the two).
    /// </summary>
    public Guid? DirectParticipantBId { get; set; }

    public DateTime? LastMessageAt { get; set; }
}