using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[Route("api/orgs/{orgId:guid}/invites")]
[ApiController]
[Authorize]
public class OrganizationInviteController : ControllerBase
{
    private readonly IServiceManager _services;

    public OrganizationInviteController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paginated list of pending (not used, not expired) invites for the organization.
    /// Optional query params: search (email), role, pageNumber, pageSize.
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<ActionResult<PagedResult<OrganizationInviteListItemDto>>> GetPendingInvites([FromRoute] Guid orgId, [FromQuery] InviteQueryParams queryParams)
    {
        var result = await _services.OrganizationInviteService.GetPendingInvitesAsync(orgId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends an email invite to the given address to join the specified organization.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<IActionResult> SendInvite([FromRoute] Guid orgId, [FromBody] SendInviteDto dto)
    {
        await _services.OrganizationInviteService.SendInviteAsync(orgId, dto);
        return Ok("Your Invite send successfully");
    }

    /// <summary>
    /// Cancel the active organization invite.
    /// </summary>
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_INVITES_MANAGE)]
    [HttpDelete("{inviteId:guid}")]
    public async Task<IActionResult> CancelInvite(Guid orgId, Guid inviteId)
    {
        await _services.OrganizationInviteService.CancelInviteAsync(orgId, inviteId);
        return Ok("Invite canceled successfully");
    }
}
