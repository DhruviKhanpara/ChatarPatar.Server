using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationInviteRepository : IBaseRepository<OrganizationInvite>
{
    /// <summary>
    /// Return Invite of provided Id in provided Organization Id
    /// </summary>
    IQueryable<OrganizationInvite> GetByIdInOrg(Guid id, Guid orgId);

    /// <summary>
    /// Returns valid pending (not used, not expired) invites for token.
    /// </summary>
    IQueryable<OrganizationInvite> GetPendingByToken(string token);


    /// <summary>
    /// Returns valid pending (not used, not expired) invites for email.
    /// </summary>
    IQueryable<OrganizationInvite> GetPendingByEmail(string email);


    /// <summary>
    /// Returns valid pending (not used, not expired) invites for email in org.
    /// </summary>
    IQueryable<OrganizationInvite> GetActiveInviteAsync(Guid orgId, string email);

    /// <summary>
    /// Returns pending (not used, not expired) invites for an org with the inviter's User loaded.
    /// Optionally filters by email search term and/or role.
    /// Caller applies pagination via PaginationExtensions.
    /// </summary>
    IQueryable<OrganizationInvite> GetPendingInvitesQuery(Guid orgId, string? search = null, OrganizationRoleEnum? role = null);
}
