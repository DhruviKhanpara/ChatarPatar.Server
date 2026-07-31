using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Common.SignalR.Model;

namespace ChatarPatar.API.SignalR.Hubs;

public interface IChatClient
{
    // ── Messages ────────────────────────────────────────────────────────────
    Task MessageReceived(MessageDto message);
    Task MessageEdited(MessageDto message);
    Task MessageDeleted(Guid messageId, Guid channelOrConversationId, bool isChannel);
    Task ReactionToggled(Guid messageId, MessageReactionToggleResultDto result);
    Task MessagePinned(PinnedMessageResponseDto pin);

    // ── Delivery / seen state (Direct DM + small Group DM only) ────────────
    Task MessageDelivered(MessageDeliveredPush payload);
    Task MessageSeen(MessageSeenPush payload);

    // ── Typing ──────────────────────────────────────────────────────────────
    Task UserTyping(Guid channelOrConversationId, Guid userId, string userName, bool isTyping);

    // ── Presence (user:{userId} + presence:{userId} groups) ───────────────
    Task UserPresenceChanged(PresencePush presence);

    Task NotificationReceived(NotificationPush notification);
    Task ReadStateUpdated(ReadStatePush badge);
    Task ThreadUpdated(ThreadUpdatePush update);
}
