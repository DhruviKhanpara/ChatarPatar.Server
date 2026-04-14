using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IServiceManager _services;

    public OrganizationController(IServiceManager services)
    {
        _services = services;
    }

    #region Organization

    /// <summary>
    /// Returns all organizations the current user is a member of, along with their role and join date in each.
    /// </summary>
    [HttpGet("my")]
    [SkipPermission]
    public async Task<ActionResult<List<OrganizationWithRoleDto>>> GetMyOrganizations()
    {
        var result = await _services.OrganizationService.GetMyOrganizationsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Returns the details of a specific organization. Caller must be a member.
    /// </summary>
    [HttpGet("{orgId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<OrganizationDto>> GetOrganization([FromRoute] Guid orgId)
    {
        var result = await _services.OrganizationService.GetOrganizationAsync(orgId);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new organization. The logged-in user becomes the OrgOwner.
    /// </summary>
    [HttpPost]
    [SkipPermission]
    public async Task<IActionResult> CreateOrganization(CreateOrganizationDto dto)
    {
        await _services.OrganizationService.CreateOrganizationAsync(dto);
        return Ok("Organization created successfully");
    }

    /// <summary>
    /// Uploads / replaces the organization logo.
    /// </summary>
    [HttpPatch("{orgId:guid}/icon")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateOrganizationLogo([FromRoute] Guid orgId, [FromForm] ImageUploadDto dto)
    {
        await _services.OrganizationService.UpdateLogoAsync(orgId, dto);
        return Ok("Organization logo updated successfully");
    }

    /// <summary>
    /// Updates the organization name or other settings.
    /// </summary>
    [HttpPatch("{orgId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_SETTINGS_EDIT)]
    public async Task<IActionResult> UpdateOrganization([FromRoute] Guid orgId, [FromBody] UpdateOrganizationDto dto)
    {
        await _services.OrganizationService.UpdateOrganizationAsync(orgId, dto);
        return Ok("Organization updated successfully");
    }

    #endregion

    #region Organization Invite

    /// <summary>
    /// Returns a paginated list of pending (not used, not expired) invites for the organization.
    /// Optional query params: search (email), role, pageNumber, pageSize.
    /// </summary>
    [HttpGet("{orgId:guid}/invites")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<ActionResult<PagedResult<OrganizationInviteListItemDto>>> GetPendingInvites([FromRoute] Guid orgId, [FromQuery] InviteQueryParams queryParams)
    {
        var result = await _services.OrganizationInviteService.GetPendingInvitesAsync(orgId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Sends an email invite to the given address to join the specified organization.
    /// </summary>
    [HttpPost("{orgId:guid}/invites")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<IActionResult> SendInvite([FromRoute] Guid orgId, [FromBody] SendInviteDto dto)
    {
        await _services.OrganizationInviteService.SendInviteAsync(orgId, dto);
        return Ok("Your Invite send successfully");
    }

    #endregion

    #region Organization Membership

    /// <summary>
    /// Returns all active members of the organization. Caller must be a member.
    /// Optional query params: search (name/username), role, pageNumber, pageSize.
    /// </summary>
    [HttpGet("{orgId:guid}/members")]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<OrganizationMemberDto>>> GetMembers( [FromRoute] Guid orgId, [FromQuery] MemberQueryParams queryParams)
    {
        var result = await _services.OrganizationMemberService.GetMembersAsync(orgId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single membership record. Caller must be a member of the org.
    /// </summary>
    [HttpGet("{orgId:guid}/members/{membershipId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<OrganizationMemberDto>> GetMember([FromRoute] Guid orgId, [FromRoute] Guid membershipId)
    {
        var result = await _services.OrganizationMemberService.GetMemberAsync(orgId, membershipId);
        return Ok(result);
    }

    /// <summary>
    /// Adds an existing registered user directly to the organization.
    /// </summary>
    [HttpPost("{orgId:guid}/members")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<IActionResult> AddOrganizationMember([FromRoute] Guid orgId, [FromBody] AddOrganizationMemberDto dto)
    {
        await _services.OrganizationMemberService.AddOrganizationMemberAsync(orgId, dto);
        return Ok("Member added to organization successfully");
    }

    /// <summary>
    /// Updates the role of an existing organization member.
    /// </summary>
    [HttpPatch("{orgId:guid}/members/{membershipId:guid}/role")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_ROLE_CHANGE)]
    public async Task<IActionResult> UpdateOrganizationMemberRole([FromRoute] Guid orgId, [FromRoute] Guid membershipId, [FromBody] UpdateOrganizationMemberRoleDto dto)
    {
        await _services.OrganizationMemberService.UpdateOrganizationMemberRole(orgId, membershipId, dto);
        return Ok("Member role updated successfully");
    }

    /// <summary>
    /// Removes a member from the organization (soft delete). Owners cannot be removed.
    /// </summary>
    [HttpDelete("{orgId:guid}/members/{membershipId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_REMOVE)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid orgId, [FromRoute] Guid membershipId)
    {
        await _services.OrganizationMemberService.RemoveMemberAsync(orgId, membershipId);
        return Ok("Member removed from organization successfully");
    }

    #endregion
}
