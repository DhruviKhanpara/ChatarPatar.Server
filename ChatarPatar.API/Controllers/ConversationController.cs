using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IServiceManager _services;

    public ConversationController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns all conversations (Direct + Group) the caller participates in.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<ConversationDto>>> GetConversations([FromQuery] PaginationParams paginationParams)
    {
        var result = await _services.ConversationService.GetConversationsAsync(paginationParams);
        return Ok(result);
    }

    /// <summary>
    /// Checks whether a Direct DM already exists with the target user.
    /// Always returns the peer's profile.
    /// </summary>
    [HttpGet("direct")]
    [SkipPermission]
    public async Task<ActionResult<DirectConversationLookupDto>> LookupDirectConversation([FromQuery] Guid userId)
    {
        var result = await _services.ConversationService.LookupDirectConversationAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single conversation by id. Caller must be a participant.
    /// </summary>
    [HttpGet("{conversationId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<ConversationDto>> GetConversation([FromRoute] Guid conversationId)
    {
        var result = await _services.ConversationService.GetConversationAsync(conversationId);
        return Ok(result);
    }

    /// <summary>
    /// Creates a Direct DM with the target user.
    /// returns the existing conversation if one already exists.
    /// </summary>
    [HttpPost("direct")]
    [SkipPermission]
    public async Task<ActionResult<ConversationDto>> CreateDirectConversation([FromBody] CreateDirectConversationDto dto)
    {
        var result = await _services.ConversationService.CreateDirectConversationAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new Group DM.
    /// Caller is automatically added as GroupAdmin.
    /// </summary>
    [HttpPost("group")]
    [SkipPermission]
    public async Task<ActionResult<ConversationDto>> CreateGroupConversation([FromBody] CreateGroupConversationDto dto)
    {
        var result = await _services.ConversationService.CreateGroupConversationAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Uploads / replaces the Group conversation Logo.
    /// </summary>
    [HttpPatch("{conversationId:guid}/logo")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateGroupConversationLogo([FromRoute] Guid conversationId, [FromForm] ImageUploadDto dto)
    {
        await _services.ConversationService.UpdateGroupConversationLogoAsync(conversationId, dto);
        return Ok("Group conversation logo updated successfully");
    }

    /// <summary>
    /// Renames a Group DM.
    /// </summary>
    [HttpPatch("{conversationId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateGroupConversation([FromRoute] Guid conversationId, [FromBody] UpdateGroupConversationDto dto)
    {
        await _services.ConversationService.UpdateGroupConversationAsync(conversationId, dto);
        return Ok("Conversation updated successfully.");
    }

    /// <summary>
    /// Remove the Group conversation logo.
    /// </summary>
    [HttpDelete("{conversationId:guid}/logo")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_SETTINGS_EDIT)]
    public async Task<IActionResult> RemoveGroupConversationLogo([FromRoute] Guid conversationId)
    {
        await _services.ConversationService.RemoveGroupConversationLogoAsync(conversationId);
        return Ok("Group conversation logo removed successfully");
    }
}
