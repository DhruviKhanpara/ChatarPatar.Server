using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.ConversationParticipant;

public class ConversationParticipantDto
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? AvatarThumbnailUrl { get; set; }
    public ConversationParticipantRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? RejoinedAt { get; set; }
}
