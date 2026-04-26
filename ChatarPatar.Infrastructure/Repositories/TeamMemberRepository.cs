using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class TeamMemberRepository : BaseSoftDeleteRepository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<TeamMember> GetTeamMemberAsync(Guid userId, Guid teamId) =>
        FindByCondition(x => x.UserId == userId && x.TeamId == teamId);

    public IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId) =>
        FindByCondition(m => m.Id == membershipId && m.TeamId == teamId)
            .Include(m => m.User)
                .ThenInclude(u => u.AvatarFile);

    public IQueryable<TeamMember> GetAllActiveMembershipsForUserInOrgQuery(Guid userId, Guid orgId) =>
        FindByCondition(m => m.UserId == userId && m.Team.OrgId == orgId);

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

    public IQueryable<TeamMember> GetMembershipsByUserInOrg(Guid userId, Guid orgId) =>
        FindByCondition(m => m.UserId == userId && m.Team.OrgId == orgId)
            .Include(m => m.Team)
                .ThenInclude(t => t.IconFile)
            .OrderBy(m => m.JoinedAt);

    public async Task<List<SoleAdminTeamResult>> GetSoleAdminTeamsWithNextSeniorMemberAsync(Guid userId, Guid orgId)
    {
        return await FindByCondition(m => m.Team.OrgId == orgId && !m.IsDeleted)
            .GroupBy(m => new { m.TeamId, m.Team.Name })
            .Where(g =>
                g.Count(x => x.Role == TeamRoleEnum.TeamAdmin && !x.IsDeleted) == 1 &&
                g.Any(x => x.UserId == userId && x.Role == TeamRoleEnum.TeamAdmin && !x.IsDeleted))
            .Select(g => new SoleAdminTeamResult(
                g.Key.TeamId,
                g.Key.Name,
                g.Where(x => x.UserId != userId && !x.IsDeleted)
                 .OrderBy(x => x.JoinedAt)
                 .Select(x => (Guid?)x.UserId)
                 .FirstOrDefault()
            ))
            .ToListAsync();
    }
}