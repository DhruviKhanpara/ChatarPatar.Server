namespace ChatarPatar.Application.DTOs.Conversation;

public class CreateGroupConversationDto
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// UserIds to add (excluding caller, who is added as GroupAdmin automatically). Min 2.
    /// </summary>
    public List<Guid> ParticipantUserIds { get; set; } = new();
}
