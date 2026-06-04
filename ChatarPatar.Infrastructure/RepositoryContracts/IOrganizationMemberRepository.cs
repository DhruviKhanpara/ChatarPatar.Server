using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationMemberRepository : IBaseSoftDeleteRepository<OrganizationMember>
{
    /// <summary>
    /// Returns a single active membership by its Id.
    /// </summary>
    IQueryable<OrganizationMember> GetById(Guid id);

    /// <summary>
    /// Returns a active membership by its Id in particular organization.
    /// </summary>
    IQueryable<OrganizationMember> GetByIdInOrg(Guid id, Guid orgId);

    /// <summary>
    /// Returns active membership of user of the org.
    /// </summary>
    IQueryable<OrganizationMember> GetOrgMemberAsync(Guid userId, Guid orgId);

    /// <summary>
    /// Returns active members of an org with User + AvatarFile loaded.
    /// Optionally filters by name/username search term and/or role.
    /// Caller applies pagination via PaginationExtensions.
    /// </summary>
    IQueryable<OrganizationMember> GetOrgMembersQuery(Guid orgId, string? search = null, OrganizationRoleEnum? role = null);

    /// <summary>
    /// Returns a single active membership by its Id in particular org, including User and AvatarFile.
    /// </summary>
    IQueryable<OrganizationMember> GetMemberByIdInOrg(Guid membershipId, Guid orgId);

    /// <summary>
    /// Returns all active org memberships for a user, including the Organization and its LogoFile.
    /// </summary>
    IQueryable<OrganizationMember> GetMembershipsByUserId(Guid userId);

    /// <summary>
    /// Fetches a single active membership row with an UPDLOCK + HOLDLOCK hint.
    ///
    /// UPDLOCK: prevents other transactions from acquiring an update lock on the
    ///          same row — serializes concurrent remove/leave calls for the same member.
    /// HOLDLOCK: promotes to SERIALIZABLE for this row — prevents phantom reads
    ///           (a deleted-then-recreated row appearing as active between checks).
    ///
    /// Must be called inside an open transaction. The lock is held until the
    /// transaction commits or rolls back, guaranteeing that exactly one concurrent
    /// execution proceeds through the cascade phases while others either wait or
    /// find the row already soft-deleted and bail out.
    ///
    /// Returns null when the row does not exist or is already soft-deleted.
    /// </summary>
    Task<OrganizationMember?> GetByIdWithUpdateLockAsync(Guid membershipId);
}