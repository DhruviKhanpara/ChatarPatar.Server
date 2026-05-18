namespace ChatarPatar.Application.DTOs.Conversation;

public class DirectConversationLookupDto
{
    /// <summary>
    /// Null when no DM exists yet.
    /// </summary>
    public Guid? ConversationId { get; set; }

    public bool HasExistingConversation => ConversationId.HasValue;
    public DirectPeerDto Peer { get; set; } = null!;
}
