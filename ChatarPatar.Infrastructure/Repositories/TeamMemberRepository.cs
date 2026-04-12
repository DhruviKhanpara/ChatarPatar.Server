using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class TeamMemberRepository : BaseSoftDeleteRepository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<TeamMember> GetTeamMemberAsync(Guid userId, Guid teamId) => 
        FindByCondition(x => x.UserId == userId && x.TeamId == teamId);
}