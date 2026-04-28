using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class TeamRepository : BaseSoftDeleteRepository<Team>, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }

    public IQueryable<Team> GetByIdInOrg(Guid teamId, Guid orgId) =>
        FindByCondition(t => t.Id == teamId && t.OrgId == orgId);

    public IQueryable<Team> GetTeamsQuery(Guid orgId, Guid callerId, bool callerIsOrgAdmin, string? search = null, bool? isArchived = null, bool includePrivate = true)
    {
        var query = FindByCondition(t => t.OrgId == orgId)
            .Include(t => t.IconFile)
            .AsQueryable();

        if (!includePrivate)
        {
            query = query.Where(t => !t.IsPrivate);
        }
        else if (!callerIsOrgAdmin)
        {
            query = query.Where(t =>
                !t.IsPrivate ||
                t.TeamMembers.Any(m => m.UserId == callerId && !m.IsDeleted));
        }

        if (isArchived.HasValue)
            query = query.Where(t => t.IsArchived == isArchived.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term));
        }

        return query.OrderBy(t => t.Name);
    }

    public async Task<bool> NameExistsInOrgAsync(Guid orgId, string name, Guid? excludeTeamId = null)
    {
        var query = FindByCondition(t => t.OrgId == orgId
            && t.Name.ToLower() == name.Trim().ToLower());

        if (excludeTeamId.HasValue)
            query = query.Where(t => t.Id != excludeTeamId.Value);

        return await query.AnyAsync();
    }
}

