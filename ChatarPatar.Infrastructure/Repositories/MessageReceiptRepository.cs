using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageReceiptRepository : BaseRepository<MessageReceipt>, IMessageReceiptRepository
{
    public MessageReceiptRepository(AppDbContext context) : base(context) { }

    public async Task<bool> MarkDeliveredAsync(Guid messageId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var rows = await _context.MessagesReceipts
            .Where(r => r.MessageId == messageId && r.UserId == userId && r.DeliveredAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.DeliveredAt, now)
                .SetProperty(r => r.UpdatedAt, now));

        return rows > 0;
    }

    public async Task<List<Guid>> MarkSeenUpToAsync(Guid conversationId, Guid userId, long upToSequence)
    {
        var now = DateTime.UtcNow;

        var idsToUpdate = await _context.MessagesReceipts
            .Where(r => r.UserId == userId
                && r.SeenAt == null
                && r.Message.ConversationId == conversationId
                && r.Message.SequenceNumber <= upToSequence)
            .Select(r => r.MessageId)
            .ToListAsync();

        if (idsToUpdate.Count == 0)
            return idsToUpdate;

        await _context.MessagesReceipts
            .Where(r => r.UserId == userId && idsToUpdate.Contains(r.MessageId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.SeenAt, now)
                .SetProperty(r => r.DeliveredAt, r => r.DeliveredAt ?? now)
                .SetProperty(r => r.UpdatedAt, now));

        return idsToUpdate;
    }
}
