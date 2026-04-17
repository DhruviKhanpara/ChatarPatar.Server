using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[Route("api/orgs/{orgId:guid}/teams/{teamId:guid}/members")]
[ApiController]
[Authorize]
public class TeamMemberController : ControllerBase
{
    private readonly IServiceManager _services;

    public TeamMemberController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds an existing org member to the team.
    /// The target user must already be a member of the organization.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_MEMBERS_INVITE)]
    public async Task<IActionResult> AddMember([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromBody] AddTeamMemberDto dto)
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
}
