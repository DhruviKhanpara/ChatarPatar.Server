using ChatarPatar.Common.SignalR.Model;

namespace ChatarPatar.Common.SignalR;

public interface ISignalRPushService
{
    // ── Personal feed (user:{userId} group) ───────────────────────────────

    /// <summary>
    /// Pushes updated unread/mention badge counts to a specific user.
    /// </summary>
    Task PushReadStateBadgeAsync(Guid userId, ReadStatePush badge);

    /// <summary>
    /// Pushes a new notification to a specific user's personal feed.
    /// </summary>
    Task PushNotificationAsync(Guid userId, NotificationPush notification);

    // ── Presence ──────────────────────────────────────────────────────────

    /// <summary>
    /// Broadcasts a user's presence change to their own tabs ("user:{userId}")
    /// and to anyone watching them ("presence:{userId}").
    /// Called by ChatHub on connect/disconnect and by PresenceService on explicit set.
    /// </summary>
    Task BroadcastPresenceAsync(Guid userId, PresencePush presence);

    // ── Thread counter ────────────────────────────────────────────────────

    /// <summary>
    /// Pushes a thread reply count update to a channel or conversation group.
    /// Called after IncrementReplyCountAsync so open clients refresh in place.
    /// </summary>
    Task BroadcastChannelThreadUpdateAsync(Guid channelId, ThreadUpdatePush update);
    Task BroadcastConversationThreadUpdateAsync(Guid conversationId, ThreadUpdatePush update);
}
