using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamMemberRepository : IBaseSoftDeleteRepository<TeamMember>
{
    IQueryable<TeamMember> GetTeamMemberAsync(Guid userId, Guid teamId);
}