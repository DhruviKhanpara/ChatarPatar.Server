using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IMessageService
{
    Task<CursorPagedResult<MessageDto>> GetConversationMessagesAsync(Guid conversationId, MessageQueryParams queryParams);
    Task<CursorPagedResult<MessageDto>> GetChannelMessagesAsync(Guid orgId, Guid teamId, Guid channelId, MessageQueryParams queryParams);

    Task<MessageDto> SendConversationMessageAsync(Guid conversationId, SendMessageDto dto);
    Task<MessageDto> SendChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, SendMessageDto dto);

    Task<PinnedMessageResponseDto> PinConversationMessageAsync(Guid conversationId, Guid messageId);
    Task<PinnedMessageResponseDto> PinChannelMessageAsync(Guid channelId, Guid messageId);
}
