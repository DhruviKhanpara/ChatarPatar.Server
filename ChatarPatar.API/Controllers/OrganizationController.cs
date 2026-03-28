using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IServiceManager _services;

    public OrganizationController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Sends an invite to the given email to join the specified organization.
    /// </summary>
    [HttpPost("{orgId:guid}/invites")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:members:invite")]
    public async Task<ActionResult<OrganizationInviteResponseDto>> SendInvite([FromRoute] Guid orgId, [FromBody] SendInviteDto dto)
    {
        var result = await _services.OrganizationInviteService.SendInviteAsync(orgId, dto);
        return Ok(result);
    }
}
