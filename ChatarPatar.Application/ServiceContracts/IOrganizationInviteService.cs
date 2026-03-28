using ChatarPatar.Application.DTOs.OrganizationInvite;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationInviteService
{
    Task<OrganizationInviteResponseDto> SendInviteAsync(Guid orgId, SendInviteDto dto);
}
