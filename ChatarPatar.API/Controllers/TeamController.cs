using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
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
    /// Returns a paged list of teams in the organization.
    /// Public teams are visible to all org members.
    /// Private teams are visible only to their members and org admins / owners.
    /// </summary>
    [HttpGet()]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<TeamWithRoleDto>>> GetTeams([FromRoute] Guid orgId, [FromQuery] TeamQueryParams queryParams)
    {
        var result = await _services.TeamService.GetTeamsAsync(orgId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single team.
    /// Returns 404 for private teams the caller does not belong to (privacy-preserving).
    /// </summary>
    [HttpGet("{teamId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<TeamDto>> GetTeam([FromRoute] Guid orgId, [FromRoute] Guid teamId)
    {
        var result = await _services.TeamService.GetTeamAsync(orgId, teamId);
        return Ok(result);
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
    public async Task<IActionResult> UpdateTeam([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromBody] UpdateTeamDto dto)
    {
        await _services.TeamService.UpdateTeamAsync(orgId, teamId, dto);
        return Ok("Team updated successfully");
    }

    /// <summary>
    /// Remove the team icon.
    /// </summary>
    [HttpDelete("{teamId:guid}/icon")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_SETTINGS_EDIT)]
    public async Task<IActionResult> RemoveTeamIcon([FromRoute] Guid orgId, [FromRoute] Guid teamId)
    {
        await _services.TeamService.RemoveTeamIconAsync(orgId, teamId);
        return Ok("Team icon removed successfully");
    }
}
