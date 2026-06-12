using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.SignalR;
using ChatarPatar.Common.SignalR.Model;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChatarPatar.Infrastructure.ExternalServices.Notification.Handlers;

/// <summary>
/// Outbox handler for Type = "MessageSent.Channel".
///
/// Responsibility:
///  1. Fan out ReadState (UnreadCount / MentionCount) increments to every 
///     active channel member except the sender.
///  2. Create Mention notifications for every mentioned user (except sender).
///  3. Create ThreadReply notification for the thread root's sender (if different
///     from the current sender and root sender is still an active member).
/// </summary>
internal sealed class MessageSentChannelOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => MessageSentChannelPayload.OutboxType;

    private readonly IRepositoryManager _repositories;
    private readonly ISignalRPushService _signalR;
    private readonly ILogger<MessageSentChannelOutboxHandler> _logger;

    public MessageSentChannelOutboxHandler(IRepositoryManager repositories, ILogger<MessageSentChannelOutboxHandler> logger, ISignalRPushService signalR)
    {
        _repositories = repositories;
        _logger = logger;
        _signalR = signalR;
    }

    public async Task HandleAsync(OutboxMessage message)
    {
        var payload = JsonConvert.DeserializeObject<MessageSentChannelPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Could not deserialize payload for outbox message {message.Id}.");

        _logger.LogInformation("[OUTBOX] Processing MessageSent.Channel — MessageId={MessageId} ChannelId={ChannelId}", payload.MessageId, payload.ChannelId);

        // ── Resolve the full member set for this channel ───────────────────

        // Fetch the channel to get TeamId and IsPrivate
        var channel = await _repositories.ChannelRepository
            .FindByCondition(c => c.Id == payload.ChannelId)
            .Select(c => new { c.TeamId, c.IsPrivate })
            .FirstOrDefaultAsync();

        if (channel is null)
        {
            _logger.LogWarning("[OUTBOX] Channel {ChannelId} not found. Skipping ReadState fanout.", payload.ChannelId);
            return;
        }

        // Explicit channel member IDs (covers private + public with explicit rows)
        var allMemberIds = await _repositories.ChannelMemberRepository
            .FindByCondition(m => m.ChannelId == payload.ChannelId && m.UserId != payload.SenderId && !m.User.IsDeleted)
            .Select(m => m.UserId)
            .ToHashSetAsync();

        // For public channels also include all team members not already listed
        if (!channel.IsPrivate)
        {
            var teamMemberIds = await _repositories.TeamMemberRepository
                .FindByCondition(m => m.TeamId == channel.TeamId && m.UserId != payload.SenderId && !m.IsDeleted && !m.User.IsDeleted)
                .Select(m => m.UserId)
                .ToHashSetAsync();

            allMemberIds.UnionWith(teamMemberIds);
        }

        if (allMemberIds.Count == 0)
        {
            _logger.LogInformation("[OUTBOX] No other members found for channel {ChannelId}. Nothing to update.", payload.ChannelId);
            return;
        }

        var now = DateTime.UtcNow;

        // ── Phase 1: ReadState fanout ──────────────────────────────────────

        var mentionedSet = payload.MentionedUserIds?.ToHashSet() ?? [];

        // ── Bulk-load existing ReadState rows for this channel ─────────────
        var existingReadStates = await _repositories.ReadStateRepository
            .FindByCondition(rs =>
                rs.ChannelId == payload.ChannelId &&
                allMemberIds.Contains(rs.UserId))
            .ToListAsync();

        if (existingReadStates.Count != allMemberIds.Count)
        {
            _logger.LogWarning(
                "[OUTBOX] Expected {Expected} ReadStates but found {Actual} for ChannelId={ChannelId}",
                allMemberIds.Count, existingReadStates.Count, payload.ChannelId);
        }

        // ReadState rows are guaranteed to exist for all participants.
        // They are provisioned during channel/team(for public) membership creation.
        foreach (var readState in existingReadStates)
        {
            readState.UnreadCount++;
            if (mentionedSet.Contains(readState.UserId))
                readState.MentionCount++;
            readState.UpdatedAt = now;
        }

        // ── Phase 2: Notifications ─────────────────────────────────────────

        // Mention notifications — one per mentioned user who is not the sender
        var notifications = mentionedSet
            .Where(x => x != payload.SenderId && allMemberIds.Contains(x))
            .Select(x => new NotificationEntity()
            {
                RecipientId = x,
                Type = NotificationTypeEnum.Mention,
                ActorId = payload.SenderId,
                MessageId = payload.MessageId,
                ChannelId = payload.ChannelId,
                Preview = payload.ContentPreview,
                IsRead = false,
                CreatedAt = now
            })
            .ToList();

        // ThreadReply notification — notify the thread root's sender once
        if (payload.ThreadRootMessageId.HasValue
            && payload.ThreadRootSenderId.HasValue
            && payload.ThreadRootSenderId.Value != payload.SenderId
            && allMemberIds.Contains(payload.ThreadRootSenderId.Value))
        {
            notifications.Add(new NotificationEntity
            {
                RecipientId = payload.ThreadRootSenderId.Value,
                Type = NotificationTypeEnum.ThreadReply,
                ActorId = payload.SenderId,
                MessageId = payload.MessageId,
                ChannelId = payload.ChannelId,
                Preview = payload.ContentPreview,
                IsRead = false,
                CreatedAt = now
            });
        }

        if (notifications.Any())
            await _repositories.NotificationRepository.AddRangeAsync(notifications);


        // ── Persist all ReadState updates and notification in a single transaction ──────────
        // SaveChangesWithoutAuditAsync is used so that updating N data rows
        // does not produce N individual audit log entries — only the single
        // summary log below is written after a successful commit.
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(
                ex,
                "[OUTBOX] ReadState fanout and Notification insert failed. MessageId={MessageId} ChannelId={ChannelId}",
                payload.MessageId, payload.ChannelId);
            throw;
        }

        // Detach the ReadState entities from the change tracker after commit.
        // ReadState has a RowVersion concurrency token — leaving them tracked with
        // the pre-commit RowVersion would cause a concurrency exception if the
        // GenericOutboxProcessor's final SaveChangesAsync accidentally re-touches them.
        foreach (var readState in existingReadStates)
            _repositories.UnitOfWork.DetachEntity(readState);

        // ── Phase 4: SignalR pushes (post-commit) ──────────────────────────────

        // Push badge to each member's personal group
        foreach (var rs in existingReadStates)
        {
            try
            {
                await _signalR.PushReadStateBadgeAsync(rs.UserId, new ReadStatePush
                {
                    ChannelId = payload.ChannelId,
                    UnreadCount = rs.UnreadCount,
                    MentionCount = rs.MentionCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OUTBOX] ReadStateBadge push failed. UserId={UserId}", rs.UserId);
            }
        }

        // Push notification to each recipient's personal group
        foreach (var n in notifications)
        {
            try
            {
                await _signalR.PushNotificationAsync(n.RecipientId, new NotificationPush
                {
                    Id = n.Id,
                    Type = n.Type,
                    ActorId = n.ActorId,
                    MessageId = n.MessageId,
                    ChannelId = n.ChannelId,
                    Preview = n.Preview,
                    CreatedAt = n.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OUTBOX] NotificationPush failed. UserId={UserId}", n.RecipientId);
            }
        }

        _logger.LogInformation(
            "[OUTBOX] ReadState fanout complete — MessageId={MessageId} ChannelId={ChannelId} MembersUpdated={Count} MentionsUpdated={MentionCount}",
            payload.MessageId, payload.ChannelId, existingReadStates.Count, existingReadStates.Count(rs => mentionedSet.Contains(rs.UserId)));

        _logger.LogInformation(
            "[OUTBOX] Notifications created — MessageId={MessageId} Mentions={MentionCount} ThreadReplies={ThreadCount}",
            payload.MessageId,
            notifications.Count(n => n.Type == NotificationTypeEnum.Mention),
            notifications.Count(n => n.Type == NotificationTypeEnum.ThreadReply));
    }
}
