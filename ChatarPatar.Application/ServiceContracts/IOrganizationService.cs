using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Organization;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationService
{
    Task<OrganizationDto> GetOrganizationAsync(Guid orgId);
    Task<List<OrganizationWithRoleDto>> GetMyOrganizationsAsync();
    Task CreateOrganizationAsync(CreateOrganizationDto dto);
    Task UpdateLogoAsync(Guid orgId, ImageUploadDto dto);
    Task UpdateOrganizationAsync(Guid orgId, UpdateOrganizationDto dto);
}
