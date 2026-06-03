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
[Route("api/v{version:apiVersion}")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IServiceManager _services;

    public MessageController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of messages for a channel.
    /// Uses cursor-based pagination: pass BeforeSequence from the oldest
    /// message in the current page to load the next (older) page.
    /// </summary>
    [HttpGet("orgs/{orgId:guid}/teams/{teamId:guid}/channels/{channelId:guid}/messages")]
    [SkipPermission]
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetChannelMessages([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromQuery] MessageQueryParams queryParams)
    {
        var result = await _services.MessageService.GetChannelMessagesAsync(orgId, teamId, channelId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paged list of messages for a conversation.
    /// Uses cursor-based pagination: pass BeforeSequence from the oldest
    /// message in the current page to load the next (older) page.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    [SkipPermission]
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetConversationMessages([FromRoute] Guid conversationId, [FromQuery] MessageQueryParams queryParams)
    {
        var result = await _services.MessageService.GetConversationMessagesAsync(conversationId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message to a channel.
    /// </summary>
    [HttpPost("orgs/{orgId:guid}/teams/{teamId:guid}/channels/{channelId:guid}/messages")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromBody] SendMessageDto dto)
    {
        var result = await _services.MessageService.SendChannelMessageAsync(orgId, teamId, channelId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message to a conversation.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromRoute] Guid conversationId, [FromBody] SendMessageDto dto)
    {
        var result = await _services.MessageService.SendConversationMessageAsync(conversationId, dto);
        return Ok(result);
    }
}
