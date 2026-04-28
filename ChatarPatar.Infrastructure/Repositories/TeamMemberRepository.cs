using ChatarPatar.Common.Enums;
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

    public IQueryable<TeamMember> GetMembersQuery(Guid teamId, string? search = null, TeamRoleEnum? role = null)
    {
        var query = FindByCondition(m => m.TeamId == teamId)
            .Include(m => m.User)
                .ThenInclude(u => u.AvatarFile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.User.Name.ToLower().Contains(term) ||
                m.User.Username.ToLower().Contains(term));
        }

        if (role.HasValue)
            query = query.Where(m => m.Role == role.Value);

        return query.OrderBy(m => m.JoinedAt);
    }
}