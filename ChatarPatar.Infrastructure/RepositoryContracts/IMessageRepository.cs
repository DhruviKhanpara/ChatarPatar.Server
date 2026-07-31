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

    /// <summary>
    /// Direct (1-1) conversations only. Stamps DmDeliveredAt on a single message
    /// the first time the recipient's client acks it. No-ops (returns false) if
    /// already delivered or if the acking user is the sender.
    /// </summary>
    Task<bool> MarkDmDeliveredAsync(Guid messageId, Guid conversationId, Guid ackingUserId);

    /// <summary>
    /// Direct (1-1) conversations only. Stamps DmSeenAt (and backfills DmDeliveredAt
    /// if somehow still null) on every not-yet-seen message up to and including
    /// the given sequence number, sent by the other participant.
    /// Returns the ids of messages actually updated, for pushing to the sender.
    /// </summary>
    Task<List<Guid>> MarkDmSeenUpToAsync(Guid conversationId, Guid ackingUserId, long upToSequence);
}
