using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/conversations/{conversationId:guid}/messages")]
[Authorize]
public class ConversationMessageController : ControllerBase
{
    private readonly IServiceManager _services;

    public ConversationMessageController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of messages for a conversation.
    /// Uses cursor-based pagination: pass BeforeSequence from the oldest
    /// message in the current page to load the next (older) page.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetConversationMessages([FromRoute] Guid conversationId, [FromQuery] MessageQueryParams queryParams)
    {
        var result = await _services.MessageService.GetConversationMessagesAsync(conversationId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message to a conversation.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromRoute] Guid conversationId, [FromBody] SendMessageDto dto)
    {
        var result = await _services.MessageService.SendConversationMessageAsync(conversationId, dto);
        return Ok(result);
    }
}
