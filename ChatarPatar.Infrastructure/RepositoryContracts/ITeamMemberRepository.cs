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
}