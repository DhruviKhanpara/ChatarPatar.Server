using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IMessageService
{
    Task<CursorPagedResult<MessageDto>> GetConversationMessagesAsync(Guid conversationId, MessageQueryParams queryParams);
    Task<CursorPagedResult<MessageDto>> GetChannelMessagesAsync(Guid orgId, Guid teamId, Guid channelId, MessageQueryParams queryParams);
    Task<MessageDto> SendConversationMessageAsync(Guid conversationId, SendMessageDto dto);
    Task<MessageDto> SendChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, SendMessageDto dto);
}
