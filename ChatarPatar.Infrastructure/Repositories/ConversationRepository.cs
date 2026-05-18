using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ConversationRepository : BaseSoftDeleteRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context) : base(context) { }

    public IQueryable<Conversation> GetUserConversationsQuery(Guid userId)
    {
        return FindByCondition(c =>
            (c.Type == ConversationTypeEnum.Direct &&
                (c.DirectParticipantAId == userId || c.DirectParticipantBId == userId))
            ||
            (c.Type == ConversationTypeEnum.Group &&
                c.ConversationParticipants.Any(p => p.UserId == userId && !p.HasLeft)));
    }

    public IQueryable<Conversation> GetByIdForUser(Guid conversationId, Guid userId)
    {
        return FindByCondition(c =>
            c.Id == conversationId &&
            (
                (c.Type == ConversationTypeEnum.Direct &&
                    (c.DirectParticipantAId == userId || c.DirectParticipantBId == userId))
                ||
                (c.Type == ConversationTypeEnum.Group &&
                    c.ConversationParticipants.Any(p => p.UserId == userId && !p.HasLeft))
            ));
    }

    public async Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId)
    {
        var (userA, userB) = ConversationHelper.Normalize(userAId, userBId);

        return await FindByCondition(c =>
            c.Type == ConversationTypeEnum.Direct &&
            c.DirectParticipantAId == userA &&
            c.DirectParticipantBId == userB)
            .FirstOrDefaultAsync();
    }
}

