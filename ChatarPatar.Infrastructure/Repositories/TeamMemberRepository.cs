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

    public IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId) =>
        FindByCondition(m => m.Id == membershipId && m.TeamId == teamId)
            .Include(m => m.User)
                .ThenInclude(u => u.AvatarFile);

    public IQueryable<TeamMember> GetTeamMembersQuery(Guid teamId, string? search = null, TeamRoleEnum? role = null)
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

    public async Task<List<SoleAdminTeamResult>> GetSoleAdminTeamsWithNextSeniorMemberAsync(
    Guid userId, Guid orgId)
    {
        // Step 1: collect TeamIds in this org where the target user IS a TeamAdmin.
        // Step 2: from those, keep only teams where NO OTHER active TeamAdmin exists.
        // Step 3: for each qualifying team, left-project the next senior non-admin member.

        return await FindByCondition(m =>
                m.UserId == userId
                && m.Role == TeamRoleEnum.TeamAdmin
                && m.Team.OrgId == orgId
                // Sole-admin check: no other active admin exists in this team
                && !m.Team.TeamMembers.Any(other =>
                    other.UserId != userId
                    && other.Role == TeamRoleEnum.TeamAdmin
                    && !other.IsDeleted))
            .Select(m => new SoleAdminTeamResult(
                m.TeamId,
                m.Team.Name,
                // Next senior member: oldest JoinedAt, excluding the departing user
                m.Team.TeamMembers
                    .Where(tm => tm.UserId != userId && !tm.IsDeleted)
                    .OrderBy(tm => tm.JoinedAt)
                    .Select(tm => (Guid?)tm.UserId)
                    .FirstOrDefault(),
                m.Team.TeamMembers
                    .Where(tm => tm.UserId != userId && !tm.IsDeleted)
                    .OrderBy(tm => tm.JoinedAt)
                    .Select(tm => (Guid?)tm.Id)
                    .FirstOrDefault()
            ))
            .AsNoTracking()
            .ToListAsync();
    }
}