using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class PinnedMessageRepository : BaseRepository<PinnedMessage>, IPinnedMessageRepository
{
    public PinnedMessageRepository(AppDbContext context) : base(context) { }

    public IQueryable<PinnedMessage> ActivePinInChannel(Guid messageId, Guid channelId) =>
        FindByCondition(x =>
            x.MessageId == messageId
            && x.ChannelId == channelId
            && x.UnPinnedAt == null);

    public IQueryable<PinnedMessage> ActivePinInConversation(Guid messageId, Guid conversationId) =>
        FindByCondition(x =>
            x.MessageId == messageId
            && x.ConversationId == conversationId
            && x.UnPinnedAt == null);
}
