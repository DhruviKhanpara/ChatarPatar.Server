using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orgs")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IServiceManager _services;

    public OrganizationController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns all organizations the current user is a member of, along with their role and join date in each.
    /// </summary>
    [HttpGet("my")]
    [SkipPermission]
    public async Task<ActionResult<List<OrganizationWithRoleDto>>> GetMyOrganizations()
    {
        var result = await _services.OrganizationService.GetOrganizationsAsync();
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
        await _services.OrganizationService.UpdateOrganizationLogoAsync(orgId, dto);
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

    /// <summary>
    /// Remove the organization logo.
    /// </summary>
    [HttpDelete("{orgId:guid}/icon")]
    [RequirePermission(PermissionCheckLogicEnum.All, Permissions.ORG_SETTINGS_EDIT)]
    public async Task<IActionResult> RemoveOrganizationLogo([FromRoute] Guid orgId)
    {
        await _services.OrganizationService.RemoveOrganizationLogoAsync(orgId);
        return Ok("Organization logo removed successfully");
    }
}
