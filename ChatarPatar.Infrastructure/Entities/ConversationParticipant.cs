using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class ConversationParticipant : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(ConversationId))]
    public Conversation Conversation { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(AddedBy))]
    public User AddedByUser { get; set; } = null!;
    #endregion

    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }

    public ConversationParticipantRoleEnum Role { get; set; } = ConversationParticipantRoleEnum.GroupMember;
    public Guid AddedBy { get; set; }


    public bool HasLeft { get; set; }
    public DateTime? LeftAt { get; set; }

    public DateTime JoinedAt { get; set; }
}