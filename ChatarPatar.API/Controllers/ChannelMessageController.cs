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
[Route("api/v{version:apiVersion}/orgs/{orgId:guid}/teams/{teamId:guid}/channels/{channelId:guid}/messages")]
[Authorize]
public class ChannelMessageController : ControllerBase
{
    private readonly IServiceManager _services;

    public ChannelMessageController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of messages for a channel.
    /// Uses cursor-based pagination: pass BeforeSequence from the oldest
    /// message in the current page to load the next (older) page.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetChannelMessages([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromQuery] MessageQueryParams queryParams)
    {
        var result = await _services.MessageService.GetChannelMessagesAsync(orgId, teamId, channelId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message to a channel.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromBody] SendMessageDto dto)
    {
        var result = await _services.MessageService.SendChannelMessageAsync(orgId, teamId, channelId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Pin a message to a channel.
    /// </summary>
    [HttpPost("{messageId:guid}/pin")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_PIN)]
    public async Task<ActionResult<MessageDto>> PinMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId)
    {
        var result = await _services.MessageService.PinChannelMessageAsync(channelId, messageId);
        return Ok(result);
    }
}
