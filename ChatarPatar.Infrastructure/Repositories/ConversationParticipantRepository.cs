using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ConversationParticipantRepository : BaseRepository<ConversationParticipant>, IConversationParticipantRepository
{
    public ConversationParticipantRepository(AppDbContext context) : base(context) { }

    public IQueryable<ConversationParticipant> GetConvMemberAsync(Guid userId, Guid convId) => FindByCondition(x => x.UserId == userId && x.ConversationId == convId && !x.HasLeft).AsQueryable();
}