using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamMemberRepository : IBaseSoftDeleteRepository<TeamMember>
{

    /// <summary>
    /// Returns a single active membership by its id within a specific team.
    /// Includes User and AvatarFile.
    /// </summary>
    IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId);

    /// <summary>
    /// Returns active members of a team with User + AvatarFile loaded.
    /// Optionally filters by name/username search and/or role.
    /// Caller applies pagination.
    /// </summary>
    IQueryable<TeamMember> GetMembersQuery(Guid teamId, string? search = null, TeamRoleEnum? role = null);
}