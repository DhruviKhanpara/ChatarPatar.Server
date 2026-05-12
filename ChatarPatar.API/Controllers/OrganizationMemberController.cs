using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orgs/{orgId:guid}/members")]
[Authorize]
public class OrganizationMemberController : ControllerBase
{
    private readonly IServiceManager _services;

    public OrganizationMemberController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns all active members of the organization. Caller must be a member.
    /// Optional query params: search (name/username), role, pageNumber, pageSize.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<OrganizationMemberDto>>> GetMembers([FromRoute] Guid orgId, [FromQuery] MemberQueryParams queryParams)
    {
        var result = await _services.OrganizationMemberService.GetMembersAsync(orgId, queryParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single membership record. Caller must be a member of the org.
    /// </summary>
    [HttpGet("{membershipId:guid}")]
    [SkipPermission]
    public async Task<ActionResult<OrganizationMemberDto>> GetMember([FromRoute] Guid orgId, [FromRoute] Guid membershipId)
    {
        var result = await _services.OrganizationMemberService.GetOrganizationMemberAsync(orgId, membershipId);
        return Ok(result);
    }

    /// <summary>
    /// Adds an existing registered user directly to the organization.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_INVITE)]
    public async Task<IActionResult> AddOrganizationMember([FromRoute] Guid orgId, [FromBody] AddOrganizationMemberDto dto)
    {
        await _services.OrganizationMemberService.AddOrganizationMemberAsync(orgId, dto);
        return Ok("Member added to organization successfully");
    }

    /// <summary>
    /// Updates the role of an existing organization member.
    /// </summary>
    [HttpPatch("{membershipId:guid}/role")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_ROLE_CHANGE)]
    public async Task<IActionResult> UpdateOrganizationMemberRole([FromRoute] Guid orgId, [FromRoute] Guid membershipId, [FromBody] UpdateOrganizationMemberRoleDto dto)
    {
        await _services.OrganizationMemberService.UpdateOrganizationMemberRoleAsync(orgId, membershipId, dto);
        return Ok("Member role updated successfully");
    }

    /// <summary>
    /// Transfer ownership to any of one member from organization
    /// </summary>
    [HttpPatch("{membershipId:guid}/transfer-ownership")]
    [SkipPermission]
    public async Task<IActionResult> TransferOwnership([FromRoute] Guid orgId, [FromRoute] Guid membershipId)
    {
        await _services.OrganizationMemberService.TransferOrganizationOwnershipAsync(orgId, membershipId);
        return Ok("Ownership transfer successfully");
    }

    /// <summary>
    /// Removes a member from the organization (soft delete). Owners cannot be removed.
    /// </summary>
    [HttpDelete("{membershipId:guid}")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_MEMBERS_REMOVE)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid orgId, [FromRoute] Guid membershipId)
    {
        await _services.OrganizationMemberService.RemoveMemberAsync(orgId, membershipId);
        return Ok("Member removed from organization successfully");
    }

    /// <summary>
    /// Leave the organization (soft delete). Owners cannot be removed.
    /// </summary>
    [HttpDelete("me")]
    [SkipPermission]
    public async Task<IActionResult> LeaveOrganization([FromRoute] Guid orgId)
    {
        await _services.OrganizationMemberService.LeaveOrganizationAsync(orgId);
        return Ok("Left the organization successfully.");
    }
}
