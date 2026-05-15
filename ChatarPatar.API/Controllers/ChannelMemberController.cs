using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.ChannelMember;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orgs/{orgId:guid}/teams/{teamId:guid}/channels/{channelId:guid}/members")]
[Authorize]
public class ChannelMemberController : ControllerBase
{
    private readonly IServiceManager _services;

    public ChannelMemberController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of members of the channel.
    /// Caller must be a member of the channel (if private) or the team (if public).
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<ChannelMemberDto>>> GetMembers([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _services.ChannelMemberService.GetMembersAsync(orgId, teamId, channelId, paginationParams);
        return Ok(result);
    }

    /// <summary>
    /// Adds an existing team member to the private channel.
    /// The target user must already be a member of the team.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.CHANNEL_MEMBERS_ADD)]
    public async Task<IActionResult> AddChannelMember([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromBody] AddChannelMemberDto dto)
    {
        await _services.ChannelMemberService.AddChannelMemberAsync(orgId, teamId, channelId, dto);
        return Ok("Member added to channel successfully.");
    }

    /// <summary>
    /// Updates the role of an existing channel member.
    /// </summary>
    [HttpPatch("{membershipId:guid}/role")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.CHANNEL_MEMBERS_ROLE_CHANGE)]
    public async Task<IActionResult> UpdateChannelMemberRole([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid membershipId, [FromBody] UpdateChannelMemberRoleDto dto)
    {
        await _services.ChannelMemberService.UpdateChannelMemberRoleAsync(orgId, teamId, channelId, membershipId, dto);
        return Ok("Member role updated successfully.");
    }

    /// <summary>
    /// Leave the channel (soft delete own membership).
    /// </summary>
    [HttpDelete("me")]
    [SkipPermission]
    public async Task<IActionResult> LeaveChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId)
    {
        await _services.ChannelMemberService.LeaveChannelAsync(orgId, teamId, channelId);
        return Ok("Left channel successfully.");
    }

    /// <summary>
    /// Removes a member from the channel.
    /// Cannot remove yourself — use the me endpoint instead.
    /// IMPORTANT: :guid constraint prevents "/me" from matching this endpoint.
    /// </summary>
    [HttpDelete("{membershipId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.CHANNEL_MEMBERS_REMOVE)]
    public async Task<IActionResult> RemoveChannelMember([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromRoute] Guid membershipId)
    {
        await _services.ChannelMemberService.RemoveChannelMemberAsync(orgId, teamId, channelId, membershipId);
        return Ok("Member removed from channel successfully.");
    }
}
