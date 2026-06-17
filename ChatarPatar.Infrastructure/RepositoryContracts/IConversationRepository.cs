using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IConversationRepository : IBaseSoftDeleteRepository<Conversation>
{
    /// <summary>
    /// Returns all conversations the user is part of:
    ///   Direct  → where DirectUser1Id or DirectUser2Id = userId
    ///   Group   → where an active ConversationParticipant row exists
    /// </summary>
    IQueryable<Conversation> GetUserConversationsQuery(Guid userId);

    /// <summary>
    /// Returns a single conversation visible to the user, or an empty queryable.
    /// Works for both Direct (checks DirectUser columns) and Group (checks participants).
    /// </summary>
    IQueryable<Conversation> GetByIdForUser(Guid conversationId, Guid userId);

    /// <summary>
    /// Returns the existing Direct conversation between two users, or null.
    /// Pass the two IDs in any order — the method normalizes them internally.
    /// </summary>
    Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId);

    /// <summary>
    /// Checks whether a user is an active participant of a conversation:
    ///   - Direct → user must be DirectParticipantAId or DirectParticipantBId
    ///   - Group  → user must have an active (HasLeft = false) ConversationParticipant row
    /// </summary>
    Task<bool> IsActiveParticipantAsync(Guid userId, Guid conversationId);
}
