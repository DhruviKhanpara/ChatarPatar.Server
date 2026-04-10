using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
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

    /// <summary>
    /// Upload org logo, from login user account
    /// </summary>
    [HttpPatch("{orgId:guid}/icon")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:settings:edit")]
    public async Task<IActionResult> UpdateOrganizationLogo([FromRoute] Guid orgId, [FromForm] ImageUploadDto dto)
    {
        await _services.OrganizationService.UpdateLogoAsync(orgId, dto);
        return Ok("Update Organization Logo successfully");
    }

    /// <summary>
    /// Update Organization
    /// </summary>
    [HttpPatch("{orgId:guid}")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:settings:edit")]
    public async Task<IActionResult> UpdateOrganization([FromRoute] Guid orgId, [FromBody] UpdateOrganizationDto dto)
    {
        await _services.OrganizationService.UpdateOrganizationAsync(orgId, dto);
        return Ok("Update Organization successfully");
    }

    #endregion

    #region Organization Invite

    /// <summary>
    /// Sends an invite to the given email to join the specified organization.
    /// </summary>
    [HttpPost("{orgId:guid}/invites")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:members:invite")]
    public async Task<IActionResult> SendInvite([FromRoute] Guid orgId, [FromBody] SendInviteDto dto)
    {
        await _services.OrganizationInviteService.SendInviteAsync(orgId, dto);
        return Ok("Your Invite send successfully");
    }

    #endregion

    #region Organization Membership

    /// <summary>
    /// Create new organization member
    /// </summary>
    [HttpPost("{orgId:guid}/members/create")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:members:invite")]
    public async Task<IActionResult> AddOrganizationMember([FromRoute] Guid orgId, [FromBody] AddOrganizationMemberDto dto)
    {
        await _services.OrganizationMemberService.AddOrganizationMemberAsync(orgId, dto);
        return Ok("Member added in Organization successfully");
    }

    /// <summary>
    /// Update Organization member role
    /// </summary>
    [HttpPatch("{orgId:guid}/members/{membershipId:guid}/role")]
    [Authorize]
    [RequirePermission(PermissionCheckLogicEnum.All, "org:members:role:change")]
    public async Task<IActionResult> UpdateOrganizationMemberRole([FromRoute] Guid orgId, [FromRoute] Guid membershipId, [FromBody] UpdateOrganizationMemberRoleDto dto)
    {
        await _services.OrganizationMemberService.UpdateOrganizationMemberRole(orgId, membershipId, dto);
        return Ok("Member role update in Organization successfully");
    }

    #endregion
}
