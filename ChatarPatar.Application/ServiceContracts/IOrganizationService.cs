using ChatarPatar.Application.DTOs.Organization;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationService
{
    Task CreateOrganizationAsync(CreateOrganizationDto dto);
}
