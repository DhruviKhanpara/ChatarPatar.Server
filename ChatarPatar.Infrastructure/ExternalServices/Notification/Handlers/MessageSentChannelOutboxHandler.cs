using ChatarPatar.Common.Models;
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
/// Responsibility: fan out ReadState (UnreadCount / MentionCount) increments
/// to every active member of the channel except the sender.
/// </summary>
internal sealed class MessageSentChannelOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => MessageSentChannelPayload.OutboxType;

    private readonly IRepositoryManager _repositories;
    private readonly ILogger<MessageSentChannelOutboxHandler> _logger;

    public MessageSentChannelOutboxHandler(IRepositoryManager repositories, ILogger<MessageSentChannelOutboxHandler> logger)
    {
        _repositories = repositories;
        _logger = logger;
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
            .FindByCondition(m => m.ChannelId == payload.ChannelId)
            .Select(m => m.UserId)
            .ToHashSetAsync();

        // For public channels also include all team members not already listed
        if (!channel.IsPrivate)
        {
            var teamMemberIds = await _repositories.TeamMemberRepository
                .FindByCondition(m => m.TeamId == channel.TeamId && !m.IsDeleted)
                .Select(m => m.UserId)
                .ToHashSetAsync();

            allMemberIds.UnionWith(teamMemberIds);
        }

        // Remove sender — they don't get an unread bump for their own message
        allMemberIds.Remove(payload.SenderId);

        if (allMemberIds.Count == 0)
        {
            _logger.LogInformation("[OUTBOX] No other members found for channel {ChannelId}. Nothing to update.", payload.ChannelId);
            return;
        }

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
                allMemberIds.Count,
                existingReadStates.Count,
                payload.ChannelId);
        }

        // ReadState rows are guaranteed to exist for all participants.
        // They are provisioned during channel/team(for public) membership creation.
        foreach (var readState in existingReadStates)
        {
            readState.UnreadCount++;
            if (mentionedSet.Contains(readState.UserId))
                readState.MentionCount++;
            
            readState.UpdatedAt = DateTime.UtcNow;
        }

        // ── Persist all ReadState updates in a single transaction ──────────
        // SaveChangesWithoutAuditAsync is used so that updating N ReadState rows
        // does not produce N individual audit log entries — only the single
        // summary log below is written after a successful commit.
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();

        try
        {
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(
                ex,
                "[OUTBOX] Transaction rolled back — ReadState fanout failed. MessageId={MessageId} ChannelId={ChannelId}",
                payload.MessageId,
                payload.ChannelId);
            throw;
        }

        // Detach the ReadState entities from the change tracker after commit.
        // ReadState has a RowVersion concurrency token — leaving them tracked with
        // the pre-commit RowVersion would cause a concurrency exception if the
        // GenericOutboxProcessor's final SaveChangesAsync accidentally re-touches them.
        foreach (var readState in existingReadStates)
            _repositories.UnitOfWork.DetachEntity(readState);

        _logger.LogInformation(
            "[OUTBOX] ReadState fanout complete — MessageId={MessageId} ChannelId={ChannelId} MembersUpdated={Count} MentionsUpdated={MentionCount}",
            payload.MessageId,
            payload.ChannelId,
            existingReadStates.Count,
            existingReadStates.Count(rs => mentionedSet.Contains(rs.UserId)));

        // TODO: use QueueManualAuditLog and log this in audit table
        _logger.LogInformation("[OUTBOX] ReadState updated for {Count} members of channel {ChannelId}.", existingReadStates.Count, payload.ChannelId);
    }
}
