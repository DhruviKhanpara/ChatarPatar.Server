using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ReadStateRepository : BaseRepository<ReadState>, IReadStateRepository
{
    private sealed record CursorSnapshot(Guid? MessageId, long Sequence);

    public ReadStateRepository(AppDbContext context) : base(context) { }

    public async Task SeedForChannelAsync(Guid userId, Guid channelId, bool isNew = false)
    {
        // Start the cursor at the current max so history is not shown as unread
        CursorSnapshot cursor;
        if (isNew)
        {
            var globalSequence = await GetGlobalSequenceMaxAsync();
            cursor = new CursorSnapshot(null, globalSequence);
        }
        else
        {
            cursor = await GetLatestCursorAsync(channelId);
        }

        await AddAsync(new ReadState
        {
            UserId = userId,
            ChannelId = channelId,

            LastReadSequenceNumber = cursor.Sequence,
            LastReadMessageId = cursor.MessageId,

            UnreadCount = 0,
            MentionCount = 0,
            LastReadAt = DateTime.UtcNow
        });
    }

    public async Task SeedForChannelsAsync(IEnumerable<Guid> userIds, IEnumerable<Guid> channelIds, bool isNew = false)
    {
        var users = userIds.Distinct().ToList();
        var channels = channelIds.Distinct().ToList();

        if (!users.Any() || !channels.Any())
            return;

        Dictionary<Guid, CursorSnapshot> cursors = [];
        long globalSequence = 0;

        if (isNew)
        {
            globalSequence = await GetGlobalSequenceMaxAsync();
        }
        else
        {
            cursors = await GetLatestChannelsCursorsAsync(channels);
        }

        var now = DateTime.UtcNow;

        var readStates = new List<ReadState>(users.Count * channels.Count);

        foreach (var userId in users)
        {
            foreach (var channelId in channels)
            {
                cursors.TryGetValue(channelId, out var cursor);

                readStates.Add(new ReadState
                {
                    UserId = userId,
                    ChannelId = channelId,

                    LastReadSequenceNumber = isNew
                        ? globalSequence
                        : cursor?.Sequence ?? 0,

                    LastReadMessageId = isNew
                        ? null
                        : cursor?.MessageId,

                    UnreadCount = 0,
                    MentionCount = 0,
                    LastReadAt = now
                });
            }
        }

        await AddRangeAsync(readStates);
    }

    public async Task SeedForConversationAsync(Guid userId, Guid conversationId, bool isNew = false)
    {
        var readState = new ReadState
        {
            UserId = userId,
            ConversationId = conversationId
        };

        await ApplyConversationCursorAsync(readState, conversationId, isNew);

        await AddAsync(readState);
    }

    public async Task ResetForConversationRejoinAsync(Guid userId, Guid conversationId)
    {
        var readState = await _context.ReadStates
            .FirstOrDefaultAsync(rs => rs.UserId == userId && rs.ConversationId == conversationId);

        if (readState is null)
        {
            await SeedForConversationAsync(userId, conversationId);
            return;
        }

        await ApplyConversationCursorAsync(readState, conversationId);
    }

    public async Task IncrementUnreadAsync(Guid userId, Guid conversationId, bool incrementMention)
    {
        await _context.ReadStates
            .Where(rs => rs.UserId == userId && rs.ConversationId == conversationId)
            .ExecuteUpdateAsync(s => incrementMention
                ? s.SetProperty(rs => rs.UnreadCount, rs => rs.UnreadCount + 1)
                 .SetProperty(rs => rs.MentionCount, rs => rs.MentionCount + 1)
                 .SetProperty(rs => rs.UpdatedAt, DateTime.UtcNow)
                : s.SetProperty(rs => rs.UnreadCount, rs => rs.UnreadCount + 1)
                 .SetProperty(rs => rs.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid? channelId, Guid? conversationId, Guid messageId, long sequenceNumber)
    {
        var rows = await FindByCondition(rs => rs.UserId == userId
                && rs.ChannelId == channelId
                && rs.ConversationId == conversationId
                && rs.LastReadSequenceNumber < sequenceNumber)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rs => rs.UnreadCount, rs => Math.Max(0, rs.UnreadCount -
                    _context.Messages.Count(m =>
                        m.ChannelId == channelId &&
                        m.ConversationId == conversationId &&
                        m.SenderId != userId &&
                        m.SequenceNumber > rs.LastReadSequenceNumber &&
                        m.SequenceNumber <= sequenceNumber)))
                .SetProperty(rs => rs.MentionCount, rs => Math.Max(0, rs.MentionCount -
                    _context.MessagesMentions.Count(mm =>
                        mm.ChannelId == channelId &&
                        mm.ConversationId == conversationId &&
                        mm.MentionedUserId == userId &&
                        mm.Message.SequenceNumber > rs.LastReadSequenceNumber &&
                        mm.Message.SequenceNumber <= sequenceNumber)))
                .SetProperty(rs => rs.LastReadSequenceNumber, sequenceNumber)
                .SetProperty(rs => rs.LastReadMessageId, messageId)
                .SetProperty(rs => rs.LastReadAt, DateTime.UtcNow)
                .SetProperty(rs => rs.UpdatedAt, DateTime.UtcNow));

        return rows > 0;
    }

    public async Task<bool> MarkAsUnreadAsync(Guid userId, Guid? channelId, Guid? conversationId, Guid messageId, long sequenceNumber)
    {
        var newCursor = sequenceNumber - 1;

        var rows = await _context.ReadStates
            .Where(rs => rs.UserId == userId
                && rs.ChannelId == channelId
                && rs.ConversationId == conversationId
                && rs.LastReadSequenceNumber > newCursor)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rs => rs.UnreadCount, _ => _context.Messages.Count(m =>
                    m.ChannelId == channelId &&
                    m.ConversationId == conversationId &&
                    m.SenderId != userId &&
                    m.SequenceNumber > newCursor))
                .SetProperty(rs => rs.MentionCount, _ => _context.MessagesMentions.Count(mm =>
                    mm.ChannelId == channelId &&
                    mm.ConversationId == conversationId &&
                    mm.MentionedUserId == userId &&
                    mm.Message.SequenceNumber > newCursor))
                .SetProperty(rs => rs.LastReadSequenceNumber, newCursor)
                .SetProperty(rs => rs.LastReadMessageId, (Guid?)null)
                .SetProperty(rs => rs.LastReadAt, DateTime.UtcNow)
                .SetProperty(rs => rs.UpdatedAt, DateTime.UtcNow));

        return rows > 0;
    }

    #region Private Section

    private async Task<long> GetGlobalSequenceMaxAsync()
    {
        return await _context.Messages
            .AsNoTracking()
            .MaxAsync(m => (long?)m.SequenceNumber) ?? 0;
    }

    private async Task<CursorSnapshot> GetLatestCursorAsync(Guid? conversationId = null, Guid? channelId = null)
    {
        if ((conversationId.HasValue && channelId.HasValue) || (!conversationId.HasValue && !channelId.HasValue))
            throw new AppException("Exactly one source must be provided.");

        var query = _context.Messages
            .Where(m => !m.IsDeleted);

        if (conversationId.HasValue)
        {
            query = query.Where(m => m.ConversationId == conversationId.Value);
        }
        else if (channelId.HasValue)
        {
            query = query.Where(m => m.ChannelId == channelId.Value);
        }

        var latest = await query
            .AsNoTracking()
            .OrderByDescending(m => m.SequenceNumber)
            .Select(m => new
            {
                m.Id,
                m.SequenceNumber
            })
            .FirstOrDefaultAsync();

        return new CursorSnapshot(latest?.Id, latest?.SequenceNumber ?? 0);
    }

    private async Task<Dictionary<Guid, CursorSnapshot>> GetLatestChannelsCursorsAsync(IEnumerable<Guid>? channelIds = null)
    {
        var channelList = channelIds?.Distinct().ToList();

        var results = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ChannelId.HasValue && channelList!.Contains(m.ChannelId.Value) && !m.IsDeleted)
            .GroupBy(m => m.ChannelId!.Value)
            .Select(g => g
                .OrderByDescending(x => x.SequenceNumber)
                .Select(x => new
                {
                    Id = g.Key,
                    x.SequenceNumber,
                    MessageId = x.Id
                })
                .First())
            .ToListAsync();

        return results.ToDictionary(x => x.Id, x => new CursorSnapshot(x.MessageId, x.SequenceNumber));
    }

    private async Task ApplyConversationCursorAsync(ReadState readState, Guid conversationId, bool isNew = false)
    {
        CursorSnapshot cursor;
        if (isNew)
        {
            var globalSequence = await GetGlobalSequenceMaxAsync();
            cursor = new CursorSnapshot(null, globalSequence);
        }
        else
        {
            cursor = await GetLatestCursorAsync(conversationId: conversationId);
        }

        readState.LastReadSequenceNumber = cursor.Sequence;
        readState.LastReadMessageId = cursor.MessageId;

        readState.UnreadCount = 0;
        readState.MentionCount = 0;
        readState.LastReadAt = DateTime.UtcNow;
    }

    #endregion
}
