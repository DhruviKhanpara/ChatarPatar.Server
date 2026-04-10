using ChatarPatar.Application.DTOs.OrganizationInvite;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationInviteService
{
    Task SendInviteAsync(Guid orgId, SendInviteDto dto);
}
