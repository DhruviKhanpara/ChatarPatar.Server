using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IConversationParticipantRepository : IBaseRepository<ConversationParticipant>
{
    IQueryable<ConversationParticipant> GetActiveParticipant(Guid userId, Guid conversationId);
    Task<ConversationParticipant?> GetByIdAsync(Guid participantId, Guid conversationId);
    Task<bool> IsActiveParticipantAsync(Guid userId, Guid conversationId);
    IQueryable<ConversationParticipant> GetActiveParticipantsQuery(Guid conversationId);
}