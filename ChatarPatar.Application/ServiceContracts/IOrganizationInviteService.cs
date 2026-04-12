using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationInviteService
{
    Task<PagedResult<OrganizationInviteListItemDto>> GetPendingInvitesAsync(Guid orgId, InviteQueryParams queryParams);
    Task SendInviteAsync(Guid orgId, SendInviteDto dto);
}
