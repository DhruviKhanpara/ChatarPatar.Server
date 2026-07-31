using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Application.DTOs.ReadState;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IMessageService
{
    Task<CursorPagedResult<MessageDto>> GetConversationMessagesAsync(Guid conversationId, MessageQueryParams queryParams);
    Task<CursorPagedResult<MessageDto>> GetChannelMessagesAsync(Guid orgId, Guid teamId, Guid channelId, MessageQueryParams queryParams);

    Task<MessageDto> SendConversationMessageAsync(Guid conversationId, SendMessageDto dto);
    Task<MessageDto> SendChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, SendMessageDto dto);

    Task<MessageDto> EditChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId, EditMessageDto dto);
    Task<MessageDto> EditConversationMessageAsync(Guid conversationId, Guid messageId, EditMessageDto dto);

    Task<MessageReactionToggleResultDto> ToggleChannelMessageReactionAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId, MessageReactionToggleDto dto);
    Task<MessageReactionToggleResultDto> ToggleConversationMessageReactionAsync(Guid conversationId, Guid messageId, MessageReactionToggleDto dto);

    Task<PinnedMessageResponseDto> PinConversationMessageAsync(Guid conversationId, Guid messageId);
    Task<PinnedMessageResponseDto> PinChannelMessageAsync(Guid channelId, Guid messageId);

    Task<ReadStateDto> MarkConversationMessageReadAsync(Guid conversationId, Guid messageId);
    Task<ReadStateDto> MarkChannelMessageReadAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId);

    /// <summary>
    /// Called from the SignalR hub (not a controller) when a recipient's client
    /// confirms it actually received a conversation message. Takes the acking
    /// user explicitly since Hub invocations don't run inside an HTTP request —
    /// IHttpContextAccessor is not reliably populated there.
    /// </summary>
    Task MarkMessageDeliveredAsync(Guid conversationId, Guid messageId, Guid ackingUserId);

    /// <summary>
    /// Called from the SignalR hub when a client acks messages as seen in real
    /// time (independent of the REST "mark read" endpoint, which stamps Seen
    /// state the same way under the hood).
    /// </summary>
    Task MarkMessagesSeenAsync(Guid conversationId, Guid upToMessageId, Guid ackingUserId);

    Task<ReadStateDto> MarkConversationMessageUnreadAsync(Guid conversationId, Guid messageId);
    Task<ReadStateDto> MarkChannelMessageUnreadAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId);

    Task DeleteChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId);
    Task DeleteConversationMessageAsync(Guid conversationId, Guid messageId);

    Task ForceDeleteChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId);
    Task ForceDeleteConversationMessageAsync(Guid conversationId, Guid messageId);
}
