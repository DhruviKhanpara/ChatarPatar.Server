using ChatarPatar.API.SignalR.Hubs;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Application.ServiceContracts.SignalR;
using ChatarPatar.Common.SignalR.Model;
using Microsoft.AspNetCore.SignalR;

namespace ChatarPatar.API.SignalR.Service;

public sealed class SignalRService : ISignalRService
{
    private readonly IHubContext<ChatHub, IChatClient> _hub;

    public SignalRService(IHubContext<ChatHub, IChatClient> hub)
    {
        _hub = hub;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ISignalRPushService — called by Infrastructure (OutboxHandler)
    // ══════════════════════════════════════════════════════════════════════

    public Task PushReadStateBadgeAsync(Guid userId, ReadStatePush badge)
        => _hub.Clients.Group($"user:{userId}").ReadStateUpdated(badge);

    public Task PushNotificationAsync(Guid userId, NotificationPush notification)
        => _hub.Clients.Group($"user:{userId}").NotificationReceived(notification);

    public async Task BroadcastPresenceAsync(Guid userId, PresencePush presence)
    {
        // Push to the user's own tabs
        await _hub.Clients.Group($"user:{userId}").UserPresenceChanged(presence);
        // Push to anyone watching this user (others who have them in their sidebar)
        await _hub.Clients.Group($"presence:{userId}").UserPresenceChanged(presence);
    }

    public Task BroadcastChannelThreadUpdateAsync(Guid channelId, ThreadUpdatePush update)
        => _hub.Clients.Group($"channel:{channelId}").ThreadUpdated(update);

    public Task BroadcastConversationThreadUpdateAsync(Guid conversationId, ThreadUpdatePush update)
        => _hub.Clients.Group($"conv:{conversationId}").ThreadUpdated(update);

    // ══════════════════════════════════════════════════════════════════════
    // ISignalRService — called by Application (MessageService etc.)
    // ══════════════════════════════════════════════════════════════════════

    public Task BroadcastChannelMessageAsync(Guid channelId, MessageDto message)
        => _hub.Clients.Group($"channel:{channelId}").MessageReceived(message);

    public Task BroadcastChannelMessageEditedAsync(Guid channelId, MessageDto message)
        => _hub.Clients.Group($"channel:{channelId}").MessageEdited(message);

    public Task BroadcastChannelMessageDeletedAsync(Guid channelId, Guid messageId, Guid deletedBy)
        => _hub.Clients.Group($"channel:{channelId}").MessageDeleted(messageId, channelId, isChannel: true);

    public Task BroadcastChannelReactionAsync(Guid channelId, Guid messageId, MessageReactionToggleResultDto result)
        => _hub.Clients.Group($"channel:{channelId}").ReactionToggled(messageId, result);

    public Task BroadcastChannelPinAsync(Guid channelId, PinnedMessageResponseDto pin)
        => _hub.Clients.Group($"channel:{channelId}").MessagePinned(pin);

    public Task BroadcastConversationMessageAsync(Guid conversationId, MessageDto message)
        => _hub.Clients.Group($"conv:{conversationId}").MessageReceived(message);

    public Task BroadcastConversationMessageEditedAsync(Guid conversationId, MessageDto message)
        => _hub.Clients.Group($"conv:{conversationId}").MessageEdited(message);

    public Task BroadcastConversationMessageDeletedAsync(Guid conversationId, Guid messageId, Guid deletedBy)
        => _hub.Clients.Group($"conv:{conversationId}").MessageDeleted(messageId, conversationId, isChannel: false);

    public Task BroadcastConversationReactionAsync(Guid conversationId, Guid messageId, MessageReactionToggleResultDto result)
        => _hub.Clients.Group($"conv:{conversationId}").ReactionToggled(messageId, result);

    public Task BroadcastConversationPinAsync(Guid conversationId, PinnedMessageResponseDto pin)
        => _hub.Clients.Group($"conv:{conversationId}").MessagePinned(pin);

    public Task BroadcastConversationMessageDeliveredAsync(Guid conversationId, MessageDeliveredPush payload)
        => _hub.Clients.Group($"conv:{conversationId}").MessageDelivered(payload);

    public Task BroadcastConversationMessageSeenAsync(Guid conversationId, MessageSeenPush payload)
        => _hub.Clients.Group($"conv:{conversationId}").MessageSeen(payload);
}
