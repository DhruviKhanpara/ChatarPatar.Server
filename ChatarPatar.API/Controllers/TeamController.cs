using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[Route("api/orgs/{orgId:guid}/teams")]
[ApiController]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly IServiceManager _services;

    public TeamController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Creates a new team within the organization. The creator is automatically added as TeamAdmin.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_TEAMS_CREATE)]
    public async Task<IActionResult> CreateTeam([FromRoute] Guid orgId, [FromBody] CreateTeamDto dto)
    {
        await _services.TeamService.CreateTeamAsync(orgId, dto);
        return Ok("Team created successfully");
    }

    /// <summary>
    /// Uploads / replaces the Team Icon.
    /// </summary>
    [HttpPatch("{teamId:guid}/icon")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateTeamIcon([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromForm] ImageUploadDto dto)
    {
        await _services.TeamService.UpdateTeamIconAsync(orgId, teamId, dto);
        return Ok("Team icon updated successfully");
    }

    /// <summary>
    /// Updates the team settings.
    /// </summary>
    [HttpPatch("{teamId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateOrganization([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromBody] UpdateTeamDto dto)
    {
        await _services.TeamService.UpdateTeamAsync(orgId, teamId, dto);
        return Ok("Team updated successfully");
    }

    /// <summary>
    /// Remove the team icon.
    /// </summary>
    [HttpDelete("{teamId:guid}/icon")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_SETTINGS_EDIT)]
    public async Task<IActionResult> RemoveOrganizationLogo([FromRoute] Guid orgId, [FromRoute] Guid teamId)
    {
        await _services.TeamService.RemoveTeamIconAsync(orgId, teamId);
        return Ok("Team icon removed successfully");
    }
}
