using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.ConversationParticipant;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/conversations/{conversationId:guid}/participants")]
[Authorize]
public class ConversationParticipantController : ControllerBase
{
    private readonly IServiceManager _services;

    public ConversationParticipantController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns all active participants of a Group conversation.
    /// Caller must be an active participant.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<ConversationParticipantDto>>> GetParticipants([FromRoute] Guid conversationId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _services.ConversationParticipantService
            .GetParticipantsAsync(conversationId, paginationParams);
        return Ok(result);
    }

    /// <summary>
    /// Adds a user to the Group. Only GroupAdmin can do this.
    /// If the user previously left, they are re-activated.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_MEMBERS_ADD)]
    public async Task<IActionResult> AddParticipant([FromRoute] Guid conversationId, [FromBody] AddConversationParticipantDto dto)
    {
        await _services.ConversationParticipantService.AddParticipantAsync(conversationId, dto);
        return Ok("Participant added successfully.");
    }

    /// <summary>
    /// Updates a participant's role
    /// An admin cannot change their own role.
    /// </summary>
    [HttpPatch("{participantId:guid}/role")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_MEMBERS_ROLE_CHANGE)]
    public async Task<IActionResult> UpdateParticipantRole([FromRoute] Guid conversationId, [FromRoute] Guid participantId, [FromBody] UpdateConversationParticipantRoleDto dto)
    {
        await _services.ConversationParticipantService.UpdateParticipantRoleAsync(conversationId, participantId, dto);
        return Ok("Participant role updated successfully.");
    }

    /// <summary>
    /// Caller leaves the Group conversation voluntarily.
    /// The last GroupAdmin must promote someone else before leaving.
    /// IMPORTANT: :guid constraint prevents "/me" matching the remove endpoint.
    /// </summary>
    [HttpDelete("me")]
    [SkipPermission]
    public async Task<IActionResult> LeaveConversation([FromRoute] Guid conversationId)
    {
        await _services.ConversationParticipantService.LeaveConversationAsync(conversationId);
        return Ok("Left conversation successfully.");
    }

    /// <summary>
    /// GroupAdmin removes another participant from the Group.
    /// Cannot remove yourself
    /// </summary>
    [HttpDelete("{participantId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.GROUP_MEMBERS_REMOVE)]
    public async Task<IActionResult> RemoveParticipant([FromRoute] Guid conversationId, [FromRoute] Guid participantId)
    {
        await _services.ConversationParticipantService.RemoveParticipantAsync(conversationId, participantId);
        return Ok("Participant removed successfully.");
    }
}
