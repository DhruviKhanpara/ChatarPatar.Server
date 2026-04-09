using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.DTOs.OrganizationMember;
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

    #region Organization

    /// <summary>
    /// Create new organization, login user is owner
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateOrganization(CreateOrganizationDto dto)
    {
        await _services.OrganizationService.CreateOrganizationAsync(dto);
        return Ok("Organization created successfully");
    }

    #endregion

    #region Organization Invite

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

    #endregion

    #region Organization Membership

    /// <summary>
    /// Create new organization, login user is owner
    /// </summary>
    [HttpPost("{orgId:guid}/add-member")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:members:invite")]
    public async Task<IActionResult> AddOrganizationMember([FromRoute] Guid orgId, [FromBody] AddOrganizationMemberDto dto)
    {
        await _services.OrganizationMemberService.AddOrganizationMemberAsync(orgId, dto);
        return Ok("Member added in Organization successfully");
    }

    #endregion
}
