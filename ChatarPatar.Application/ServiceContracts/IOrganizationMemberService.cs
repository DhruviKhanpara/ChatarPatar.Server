using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationMemberService
{
    Task<PagedResult<OrganizationMemberDto>> GetMembersAsync(Guid orgId, MemberQueryParams queryParams);
    Task<OrganizationMemberDto> GetMemberAsync(Guid orgId, Guid membershipId);
    Task AddOrganizationMemberAsync(Guid orgId, AddOrganizationMemberDto dto);
    Task UpdateOrganizationMemberRole(Guid orgId, Guid membershipId, UpdateOrganizationMemberRoleDto dto);
    Task RemoveMemberAsync(Guid orgId, Guid membershipId);
}
