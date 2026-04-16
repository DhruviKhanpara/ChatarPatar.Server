using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationInviteRepository : BaseRepository<OrganizationInvite>, IOrganizationInviteRepository
{
    public OrganizationInviteRepository(AppDbContext context) : base(context) { }

    public IQueryable<OrganizationInvite> GetByIdInOrg(Guid id, Guid orgId) =>
        FindByCondition(i => i.Id == id && i.OrganizationId == orgId);

    public IQueryable<OrganizationInvite> GetPendingByToken(string token) =>
        FindByCondition(i => i.Token == token 
            && !i.IsUsed 
            && i.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OrganizationInvite> GetPendingByEmail(string email) =>
        FindByCondition(i => i.Email == email 
            && !i.IsUsed 
            && i.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OrganizationInvite> GetActiveInviteAsync(Guid orgId, string email) =>
        FindByCondition(x => x.OrganizationId == orgId 
            && x.Email == email 
            && !x.IsUsed
            && x.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OrganizationInvite> GetPendingInvitesQuery(Guid orgId, string? search = null, OrganizationRoleEnum? role = null)
    {
        var query = FindByCondition(x => x.OrganizationId == orgId && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .Include(x => x.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(term));
        }

        if (role.HasValue)
            query = query.Where(x => x.Role == role.Value);

        return query.OrderByDescending(x => x.CreatedAt);
    }
}
