using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class TeamMemberRepository : BaseSoftDeleteRepository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId) =>
        FindByCondition(m => m.Id == membershipId && m.TeamId == teamId)
            .Include(m => m.User)
                .ThenInclude(u => u.AvatarFile);
}