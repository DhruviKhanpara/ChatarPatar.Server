using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationInviteRepository : BaseRepository<OrganizationInvite>, IOrganizationInviteRepository
{
    public OrganizationInviteRepository(AppDbContext context) : base(context) { }

    public async Task<OrganizationInvite?> GetValidInviteAsync(string token, string email) => await FindByCondition(x => x.Token == token && x.Email == email && x.IsUsed == false && x.ExpiresAt > DateTime.UtcNow).AsNoTracking().FirstOrDefaultAsync();

    public IQueryable<OrganizationInvite> GetPendingByToken(string token) =>
        FindByCondition(i => i.Token == token && !i.IsUsed && i.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OrganizationInvite> GetPendingByEmail(string email) =>
        FindByCondition(i => i.Email == email && !i.IsUsed && i.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OrganizationInvite> GetActiveInviteAsync(Guid orgId, string email) =>
        FindByCondition(x => x.OrganizationId == orgId && x.Email == email && x.IsUsed == false && x.ExpiresAt > DateTime.UtcNow);
}
