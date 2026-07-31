using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IReadStateRepository : IBaseRepository<ReadState>
{
    /// <summary>
    /// Creates a ReadState row for a new channel member seeded at the current
    /// message high-water mark, so historical messages don't appear as unread.
    /// </summary>
    Task SeedForChannelAsync(Guid userId, Guid channelId, bool isNew = false);

    /// <summary>
    /// Creates a ReadState rows for a new channel member seeded at the current
    /// message high-water mark, so historical messages don't appear as unread.
    /// </summary>
    Task SeedForChannelsAsync(IEnumerable<Guid> userIds, IEnumerable<Guid> channelIds, bool isNew = false);

    /// <summary>
    /// Creates a ReadState row for a new conversation participant seeded at
    /// the current message high-water mark.
    /// For brand-new conversations this will be sequence 0 / null messageId.
    /// </summary>
    Task SeedForConversationAsync(Guid userId, Guid conversationId, bool isNew = false);

    /// <summary>
    /// Resets an existing ReadState to the current message high-water mark
    /// when a participant re-joins a conversation they previously left.
    /// Falls back to SeedForConversationAsync if the row is missing.
    /// </summary>
    Task ResetForConversationRejoinAsync(Guid userId, Guid conversationId);

    Task IncrementUnreadAsync(Guid userId, Guid conversationId, bool incrementMention);

    Task<ReadState?> MarkAsReadAsync(Guid userId, Guid? channelId, Guid? conversationId, Guid messageId, long sequenceNumber);

    Task<ReadState?> MarkAsUnreadAsync(Guid userId, Guid? channelId, Guid? conversationId, Guid messageId, long sequenceNumber);
}
