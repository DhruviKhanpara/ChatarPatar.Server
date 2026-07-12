using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ConversationParticipantRepository : BaseRepository<ConversationParticipant>, IConversationParticipantRepository
{
    public ConversationParticipantRepository(AppDbContext context) : base(context) { }

    public IQueryable<ConversationParticipant> GetActiveParticipant(Guid userId, Guid conversationId) =>
        FindByCondition(p => p.UserId == userId && p.ConversationId == conversationId && !p.HasLeft);

    public Task<ConversationParticipant?> GetByIdAsync(Guid participantId, Guid conversationId) =>
        FindByCondition(p => p.Id == participantId && p.ConversationId == conversationId)
            .FirstOrDefaultAsync();

    public IQueryable<ConversationParticipant> GetActiveParticipantsQuery(Guid conversationId) =>
        FindByCondition(p => p.ConversationId == conversationId && !p.HasLeft);
}