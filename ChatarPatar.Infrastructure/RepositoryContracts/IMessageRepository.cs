using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IMessageRepository : IBaseRepository<Message>
{
    IQueryable<Message?> FindByClientMessageIdAsync(Guid senderId, Guid clientMessageId, Guid? channelId = null, Guid? conversationId = null);
    Task<Message?> GetThreadRootAsync(Guid messageId, Guid? channelId = null, Guid? conversationId = null);
    IQueryable<Message> GetChannelMessagesQuery(Guid channelId, long? beforeSequence, Guid? threadRootMessageId);
    IQueryable<Message> GetConversationMessagesQuery(Guid conversationId, long? beforeSequence, Guid? threadRootMessageId);
}
