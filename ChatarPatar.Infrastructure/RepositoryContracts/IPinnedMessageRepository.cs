using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IPinnedMessageRepository : IBaseRepository<PinnedMessage>
{
    IQueryable<PinnedMessage> MessagePinInChannel(Guid messageId, Guid channelId);
    IQueryable<PinnedMessage> MessagePinInConversation(Guid messageId, Guid conversationId);
    IQueryable<PinnedMessage> PinInChannel(Guid channelId);
    IQueryable<PinnedMessage> PinInConversation(Guid conversationId);
}
