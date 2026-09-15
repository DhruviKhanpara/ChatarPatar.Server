using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IMessageReceiptRepository : IBaseRepository<MessageReceipt>
{
    /// <summary>
    /// Small-group conversations only. Stamps DeliveredAt on this user's receipt
    /// row for a single message the first time their client acks it.
    /// </summary>
    Task<bool> MarkDeliveredAsync(Guid messageId, Guid userId);

    /// <summary>
    /// Small-group conversations only. Stamps SeenAt (and backfills DeliveredAt
    /// if still null) on every not-yet-seen receipt row for this user, up to and
    /// including the given sequence number. Returns the message ids updated.
    /// </summary>
    Task<List<Guid>> MarkSeenUpToAsync(Guid conversationId, Guid userId, long upToSequence);

    /// <summary>
    /// All receipt rows for a single message. Empty means the message was
    /// never receipt-tracked (channel, Direct DM, or group over threshold
    /// at send time) — not that nobody's seen it.
    /// </summary>
    IQueryable<MessageReceipt> GetForMessage(Guid messageId);
}
