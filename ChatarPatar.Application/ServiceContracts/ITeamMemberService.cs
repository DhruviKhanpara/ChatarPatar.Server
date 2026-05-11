using ChatarPatar.Application.DTOs.TeamMember;

namespace ChatarPatar.Application.ServiceContracts;

public interface ITeamMemberService
{
    Task AddTeamMemberAsync(Guid orgId, Guid teamId, AddTeamMemberDto dto);
    Task UpdateTeamMemberRoleAsync(Guid orgId, Guid teamId, Guid membershipId, UpdateTeamMemberRoleDto dto);
    Task RemoveTeamMemberAsync(Guid orgId, Guid teamId, Guid membershipId);
    Task LeaveTeamAsync(Guid orgId, Guid teamId);
}
