using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IConversationParticipantRepository : IBaseRepository<ConversationParticipant>
{
    IQueryable<ConversationParticipant> GetConvMemberAsync(Guid userId, Guid convId);
}