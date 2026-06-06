using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IPinnedMessageRepository : IBaseRepository<PinnedMessage>
{
    IQueryable<PinnedMessage> ActivePinInChannel(Guid messageId, Guid channelId);
    IQueryable<PinnedMessage> ActivePinInConversation(Guid messageId, Guid conversationId);
}
