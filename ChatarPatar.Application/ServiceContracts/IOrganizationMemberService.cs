using ChatarPatar.Application.DTOs.OrganizationMember;

namespace ChatarPatar.Application.ServiceContracts;

public interface IOrganizationMemberService
{
    Task AddOrganizationMemberAsync(Guid orgId, AddOrganizationMemberDto dto);
    Task UpdateOrganizationMemberRole(Guid orgId, Guid membershipId, UpdateOrganizationMemberRoleDto dto);
}
