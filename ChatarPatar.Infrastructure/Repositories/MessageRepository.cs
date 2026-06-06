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
}
