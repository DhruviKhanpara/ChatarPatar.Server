using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationInviteRepository : IBaseRepository<OrganizationInvite>
{
    Task<OrganizationInvite?> GetValidInviteAsync(string token, string email);
    IQueryable<OrganizationInvite> GetPendingByToken(string token);
    IQueryable<OrganizationInvite> GetPendingByEmail(string email);
    IQueryable<OrganizationInvite> GetActiveInviteAsync(Guid orgId, string email);
}
