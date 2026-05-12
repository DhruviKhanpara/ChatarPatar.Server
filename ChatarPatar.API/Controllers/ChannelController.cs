using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orgs/{orgId:guid}/teams/{teamId:guid}/channels")]
[Authorize]
public class ChannelController : ControllerBase
{
    private readonly IServiceManager _services;

    public ChannelController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of channels in the team.
    /// Public channels are visible to all team members.
    /// Private channels are visible only to their members and team/org admins.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<ChannelWithRoleDto>>> GetChannels([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromQuery] ChannelQueryParams queryParams)
    {
        var result = await _services.ChannelService.GetChannelsAsync(orgId, teamId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single channel.
    /// </summary>
    [HttpGet("{channelId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<ChannelDto>> GetChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId)
    {
        var result = await _services.ChannelService.GetChannelAsync(orgId, teamId, channelId);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new channel within the team. The creator is automatically added as ChannelModerator.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_CHANNELS_CREATE)]
    public async Task<IActionResult> CreateChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromBody] CreateChannelDto dto)
    {
        await _services.ChannelService.CreateChannelAsync(orgId, teamId, dto);
        return Ok("Channel created successfully.");
    }

    /// <summary>
    /// Updates the channel details.
    /// </summary>
    [HttpPatch("{channelId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.CHANNEL_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromBody] UpdateChannelDto dto)
    {
        await _services.ChannelService.UpdateChannelAsync(orgId, teamId, channelId, dto);
        return Ok("Channel updated successfully.");
    }

    /// <summary>
    /// Archives the channel. Archived channels are read-only.
    /// </summary>
    [HttpPost("{channelId:guid}/archive")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_CHANNELS_ARCHIVE)]
    public async Task<IActionResult> ArchiveChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId)
    {
        await _services.ChannelService.ArchiveChannelAsync(orgId, teamId, channelId);
        return Ok("Channel archived successfully.");
    }

    /// <summary>
    /// Unarchives the channel, making it active again.
    /// </summary>
    [HttpPost("{channelId:guid}/unarchive")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.TEAM_CHANNELS_ARCHIVE)]
    public async Task<IActionResult> UnarchiveChannel([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId)
    {
        await _services.ChannelService.UnarchiveChannelAsync(orgId, teamId, channelId);
        return Ok("Channel unarchived successfully.");
    }
}
