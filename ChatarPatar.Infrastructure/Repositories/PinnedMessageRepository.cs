using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class PinnedMessageRepository : BaseRepository<PinnedMessage>, IPinnedMessageRepository
{
    public PinnedMessageRepository(AppDbContext context) : base(context) { }

    public IQueryable<PinnedMessage> MessagePinInChannel(Guid messageId, Guid channelId) =>
        FindByCondition(x =>
            x.MessageId == messageId
            && x.ChannelId == channelId);

    public IQueryable<PinnedMessage> MessagePinInConversation(Guid messageId, Guid conversationId) =>
        FindByCondition(x =>
            x.MessageId == messageId
            && x.ConversationId == conversationId);

    public IQueryable<PinnedMessage> PinInChannel(Guid channelId) =>
        FindByCondition(x => x.ChannelId == channelId);

    public IQueryable<PinnedMessage> PinInConversation(Guid conversationId) =>
        FindByCondition(x => x.ConversationId == conversationId);
}
