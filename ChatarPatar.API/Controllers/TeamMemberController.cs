using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orgs/{orgId:guid}/teams/{teamId:guid}/members")]
[Authorize]
public class TeamMemberController : ControllerBase
{
    private readonly IServiceManager _services;

    public TeamMemberController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of members of the team.
    /// Caller must be a member of the team.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<TeamMemberDto>>> GetMembers([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromQuery] MemberQueryParams queryParams)
    {
        var result = await _services.TeamMemberService.GetMembersAsync(orgId, teamId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Adds an existing org member to the team.
    /// The target user must already be a member of the organization.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_MEMBERS_INVITE)]
    public async Task<IActionResult> AddTeamMember([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromBody] AddTeamMemberDto dto)
    {
        await _services.TeamMemberService.AddTeamMemberAsync(orgId, teamId, dto);
        return Ok("Member added to team successfully.");
    }

    /// <summary>
    /// Updates the role of an existing team member.
    /// </summary>
    [HttpPatch("{membershipId:guid}/role")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_MEMBERS_ROLE_CHANGE)]
    public async Task<IActionResult> UpdateTeamMemberRole([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid membershipId, [FromBody] UpdateTeamMemberRoleDto dto)
    {
        await _services.TeamMemberService.UpdateTeamMemberRoleAsync(orgId, teamId, membershipId, dto);
        return Ok("Member role updated successfully.");
    }

    /// <summary>
    /// Removes a member from the team (soft-delete).
    /// Cannot remove yourself — use the me endpoint instead.
    /// Cannot remove the last admin.
    /// </summary>
    [HttpDelete("{membershipId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_MEMBERS_KICK)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid membershipId)
    {
        await _services.TeamMemberService.RemoveTeamMemberAsync(orgId, teamId, membershipId);
        return Ok("Member removed from team successfully.");
    }

    /// <summary>
    /// Leave the Team (soft delete).
    /// </summary>
    [HttpDelete("me")]
    [SkipPermission]
    public async Task<IActionResult> LeaveTeam([FromRoute] Guid orgId, [FromRoute] Guid teamId)
    {
        await _services.TeamMemberService.LeaveTeamAsync(orgId, teamId);
        return Ok("Left the team successfully.");
    }
}
