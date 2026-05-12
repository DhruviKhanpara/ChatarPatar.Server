using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Organization;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationService
{
    Task<List<OrganizationWithRoleDto>> GetOrganizationsAsync();
    Task<OrganizationDto> GetOrganizationAsync(Guid orgId);
    Task CreateOrganizationAsync(CreateOrganizationDto dto);
    Task UpdateOrganizationLogoAsync(Guid orgId, ImageUploadDto dto);
    Task UpdateOrganizationAsync(Guid orgId, UpdateOrganizationDto dto);
    Task RemoveOrganizationLogoAsync(Guid orgId);
}
