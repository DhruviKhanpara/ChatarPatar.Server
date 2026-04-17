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

    public async Task<bool> NameExistsInOrgAsync(Guid orgId, string name, Guid? excludeTeamId = null)
    {
        var query = FindByCondition(t => t.OrgId == orgId
            && t.Name == name.Trim());

        if (excludeTeamId.HasValue)
            query = query.Where(t => t.Id != excludeTeamId.Value);

        return await query.AnyAsync();
    }
}

