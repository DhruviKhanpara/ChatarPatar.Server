using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Reaction;
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
    public async Task<ActionResult<CursorPagedResult<MessageDto>>> GetMessages([FromRoute] Guid conversationId, [FromQuery] MessageQueryParams queryParams)
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

    /// <summary>
    /// Edits a conversation message. Only the original sender may call this.
    /// Pass the full desired state: Content, FileIds (kept + new), MentionedUserIds.
    /// </summary>
    [HttpPatch("{messageId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_EDIT_OWN)]
    public async Task<ActionResult<MessageDto>> EditMessage([FromRoute] Guid conversationId, [FromRoute] Guid messageId, [FromBody] EditMessageDto dto)
    {
        var result = await _services.MessageService.EditConversationMessageAsync(conversationId, messageId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Toggles an emoji reaction on a conversation message.
    /// If the calling user has already reacted with this emoji → removes the reaction.
    /// If they have not yet reacted → adds the reaction.
    /// </summary>
    /// <returns>
    /// the action taken (Added: true/false) and the updated summary for
    /// that emoji so the client can patch its local state without re-fetching.
    /// </returns>
    [HttpPost("{messageId:guid}/reactions")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_REACT)]
    public async Task<ActionResult<MessageReactionToggleResultDto>> ToggleReaction([FromRoute] Guid conversationId, [FromRoute] Guid messageId, [FromBody] MessageReactionToggleDto dto)
    {
        var result = await _services.MessageService.ToggleConversationMessageReactionAsync(conversationId, messageId, dto);
        return Ok(result);
    }

    /// <summary>
    /// pin a message to a conversation.
    /// </summary>
    [HttpPost("{messageId:guid}/pin")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_PIN)]
    public async Task<ActionResult<MessageDto>> PinMessage([FromRoute] Guid conversationId, [FromRoute] Guid messageId)
    {
        var result = await _services.MessageService.PinConversationMessageAsync(conversationId, messageId);
        return Ok(result);
    }
    
    /// <summary>
    /// Soft-deletes the calling user's own message.
    /// Applies to both Direct and Group conversations.
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_DELETE_OWN)]
    public async Task<IActionResult> DeleteOwnMessage([FromRoute] Guid conversationId, [FromRoute] Guid messageId)
    {
        await _services.MessageService.DeleteConversationMessageAsync(conversationId, messageId);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes any member's message in a group conversation.
    /// Not available for Direct (1-to-1) conversations — the permission layer
    /// Use this for group — removing spam, offensive content, etc.
    /// Cannot be used to delete your own messages
    /// </summary>
    [HttpDelete("{messageId:guid}/force")]
    [RequirePermission(PermissionCheckLogicEnum.Any, Permissions.MESSAGE_DELETE_ANY)]
    public async Task<IActionResult> ForceDeleteMessage([FromRoute] Guid conversationId, [FromRoute] Guid messageId)
    {
        await _services.MessageService.ForceDeleteConversationMessageAsync(conversationId, messageId);
        return NoContent();
    }
}
