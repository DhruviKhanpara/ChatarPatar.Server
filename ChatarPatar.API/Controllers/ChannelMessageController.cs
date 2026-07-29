using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Application.DTOs.ReadState;
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
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetMessages([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromQuery] MessageQueryParams queryParams)
    {
        var result = await _services.MessageService.GetChannelMessagesAsync(orgId, teamId, channelId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message in a channel.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromBody] SendMessageRequest model)
    {
        var dto = new SendMessageDto
        {
            ClientMessageId = model.ClientMessageId,
            Content = model.Content,
            FileIds = model.FileIds,
            MentionedUserIds = model.MentionedUserIds,
            ThreadRootMessageId = null
        };

        var result = await _services.MessageService.SendChannelMessageAsync(orgId, teamId, channelId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Reply to a message in a channel.
    /// </summary>
    [HttpPost("{threadId:guid}/replies")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<MessageDto>> ReplyMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid threadId, [FromBody] SendMessageRequest model)
    {
        var dto = new SendMessageDto
        {
            ClientMessageId = model.ClientMessageId,
            Content = model.Content,
            FileIds = model.FileIds,
            MentionedUserIds = model.MentionedUserIds,
            ThreadRootMessageId = threadId
        };

        var result = await _services.MessageService.SendChannelMessageAsync(orgId, teamId, channelId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Edits a channel message. Only the original sender may call this.
    /// Pass the full desired state: Content, FileIds (kept + new), MentionedUserIds.
    /// </summary>
    [HttpPatch("{messageId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_EDIT_OWN)]
    public async Task<ActionResult<MessageDto>> EditMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId, [FromBody] EditMessageDto dto)
    {
        var result = await _services.MessageService.EditChannelMessageAsync(orgId, teamId, channelId, messageId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Toggles an emoji reaction on a channel message.
    /// If the calling user has already reacted with this emoji → removes the reaction.
    /// If they have not yet reacted → adds the reaction.
    /// </summary>
    /// <returns>
    /// the action taken (Added: true/false) and the updated summary for
    /// that emoji so the client can patch its local state without re-fetching.
    /// </returns>
    [HttpPost("{messageId:guid}/reactions")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_REACT)]
    public async Task<ActionResult<MessageReactionToggleResultDto>> ToggleReaction([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId, [FromBody] MessageReactionToggleDto dto)
    {
        var result = await _services.MessageService.ToggleChannelMessageReactionAsync(orgId, teamId, channelId, messageId, dto);
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

    /// <summary>
    /// Channel message - Mark as read.
    /// </summary>
    [HttpPatch("{messageId:guid}/read")]
    [SkipPermission]
    public async Task<ActionResult<ReadStateDto>> MarkRead([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId)
    {
        var result = await _services.MessageService.MarkChannelMessageReadAsync(orgId, teamId, channelId, messageId);
        return Ok(result);
    }

    /// <summary>
    /// Channel message - Mark as un-read.
    /// </summary>
    [HttpPatch("{messageId:guid}/unread")]
    [SkipPermission]
    public async Task<ActionResult<ReadStateDto>> MarkUnread([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId)
    {
        var result = await _services.MessageService.MarkChannelMessageUnreadAsync(orgId, teamId, channelId, messageId);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes the calling user's own message.
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_DELETE_OWN)]
    public async Task<IActionResult> DeleteOwnMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId)
    {
        await _services.MessageService.DeleteChannelMessageAsync(orgId, teamId, channelId, messageId);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes any member's message in a channel.
    /// Use this for moderation — removing spam, offensive content, etc.
    /// Cannot be used to delete your own messages
    /// </summary>
    [HttpDelete("{messageId:guid}/force")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_DELETE_ANY)]
    public async Task<IActionResult> ForceDeleteMessage([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid messageId)
    {
        await _services.MessageService.ForceDeleteChannelMessageAsync(orgId, teamId, channelId, messageId);
        return NoContent();
    }
}
