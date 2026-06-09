using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IMessageRepository : IBaseRepository<Message>
{
    IQueryable<Message> GetByIdInChannel(Guid messageId, Guid channelId);
    IQueryable<Message> GetByIdInConversation(Guid messageId, Guid conversationId);
    IQueryable<Message?> FindByClientMessageIdAsync(Guid senderId, Guid clientMessageId, Guid? channelId = null, Guid? conversationId = null);
    Task<Message?> GetThreadRootAsync(Guid messageId, Guid? channelId = null, Guid? conversationId = null);
    IQueryable<Message> GetChannelMessagesQuery(Guid channelId, long? beforeSequence, Guid? threadRootMessageId);
    IQueryable<Message> GetConversationMessagesQuery(Guid conversationId, long? beforeSequence, Guid? threadRootMessageId);
    Task IncrementReplyCountAsync(Guid messageId, DateTime repliedAt);
}
