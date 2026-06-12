using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Common.SignalR;

namespace ChatarPatar.Application.ServiceContracts.SignalR;

public interface ISignalRService : ISignalRPushService
{
    // ── Channel message events ────────────────────────────────────────────
    Task BroadcastChannelMessageAsync(Guid channelId, MessageDto message);
    Task BroadcastChannelMessageEditedAsync(Guid channelId, MessageDto message);
    Task BroadcastChannelMessageDeletedAsync(Guid channelId, Guid messageId, Guid deletedBy);
    Task BroadcastChannelReactionAsync(Guid channelId, Guid messageId, MessageReactionToggleResultDto result);
    Task BroadcastChannelPinAsync(Guid channelId, PinnedMessageResponseDto pin);

    // ── Conversation message events ───────────────────────────────────────
    Task BroadcastConversationMessageAsync(Guid conversationId, MessageDto message);
    Task BroadcastConversationMessageEditedAsync(Guid conversationId, MessageDto message);
    Task BroadcastConversationMessageDeletedAsync(Guid conversationId, Guid messageId, Guid deletedBy);
    Task BroadcastConversationReactionAsync(Guid conversationId, Guid messageId, MessageReactionToggleResultDto result);
    Task BroadcastConversationPinAsync(Guid conversationId, PinnedMessageResponseDto pin);
}
