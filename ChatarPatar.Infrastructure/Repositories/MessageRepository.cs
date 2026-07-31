using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageRepository : BaseRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public IQueryable<Message> GetByIdInChannel(Guid messageId, Guid channelId) =>
        FindByCondition(x => x.Id == messageId && x.ChannelId == channelId);
    
    public IQueryable<Message> GetByIdInConversation(Guid messageId, Guid conversationId) =>
        FindByCondition(x => x.Id == messageId && x.ConversationId == conversationId);

    public IQueryable<Message?> FindByClientMessageIdAsync(Guid senderId, Guid clientMessageId, Guid? channelId = null, Guid? conversationId = null)
    {
        return FindByCondition(m =>
                m.ClientMessageId == clientMessageId
                && m.SenderId == senderId
                && (channelId == null || m.ChannelId == channelId)
                && (conversationId == null || m.ConversationId == conversationId));
    }

    public Task<Message?> GetThreadRootAsync(Guid messageId, Guid? channelId = null, Guid? conversationId = null)
    {
        return FindByCondition(m =>
                m.Id == messageId
                && (channelId == null || m.ChannelId == channelId)
                && (conversationId == null || m.ConversationId == conversationId))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public IQueryable<Message> GetChannelMessagesQuery(Guid channelId, long? beforeSequence, Guid? threadRootMessageId)
    {
        var query = FindByCondition(m =>
            m.ChannelId == channelId
            && (threadRootMessageId == null
                ? m.ThreadRootMessageId == null
                : m.ThreadRootMessageId == threadRootMessageId));

        if (beforeSequence.HasValue)
            query = query.Where(m => m.SequenceNumber < beforeSequence.Value);

        return query.OrderByDescending(m => m.SequenceNumber);
    }

    public IQueryable<Message> GetConversationMessagesQuery(Guid conversationId, long? beforeSequence, Guid? threadRootMessageId)
    {
        var query = FindByCondition(m =>
            m.ConversationId == conversationId
            && (threadRootMessageId == null
                ? m.ThreadRootMessageId == null
                : m.ThreadRootMessageId == threadRootMessageId));

        if (beforeSequence.HasValue)
            query = query.Where(m => m.SequenceNumber < beforeSequence.Value);

        return query.OrderByDescending(m => m.SequenceNumber);
    }

    public async Task IncrementReplyCountAsync(Guid messageId, DateTime repliedAt)
    {
        await _context.Messages
            .Where(m => m.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ReplyCount, m => m.ReplyCount + 1)
                .SetProperty(m => m.LastReplyAt, repliedAt));
    }

    public async Task<bool> MarkDmDeliveredAsync(Guid messageId, Guid conversationId, Guid ackingUserId)
    {
        var now = DateTime.UtcNow;

        var rows = await _context.Messages
            .Where(m => m.Id == messageId
                && m.ConversationId == conversationId
                && m.SenderId != ackingUserId
                && m.DmDeliveredAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.DmDeliveredAt, now)
                .SetProperty(m => m.UpdatedAt, now));

        return rows > 0;
    }

    public async Task<List<Guid>> MarkDmSeenUpToAsync(Guid conversationId, Guid ackingUserId, long upToSequence)
    {
        var now = DateTime.UtcNow;

        var idsToUpdate = await _context.Messages
            .Where(m => m.ConversationId == conversationId
                && m.SenderId != ackingUserId
                && m.SequenceNumber <= upToSequence
                && m.DmSeenAt == null)
            .Select(m => m.Id)
            .ToListAsync();

        if (idsToUpdate.Count == 0)
            return idsToUpdate;

        await _context.Messages
            .Where(m => idsToUpdate.Contains(m.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.DmSeenAt, now)
                // Seen implies delivered — backfill in case the delivered ack was missed/skipped.
                .SetProperty(m => m.DmDeliveredAt, m => m.DmDeliveredAt ?? now)
                .SetProperty(m => m.UpdatedAt, now));

        return idsToUpdate;
    }
}
