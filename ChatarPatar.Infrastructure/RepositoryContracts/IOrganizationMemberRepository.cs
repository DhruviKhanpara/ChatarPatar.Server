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
    /// Returns active membership of user of the org.
    /// </summary>
    IQueryable<OrganizationMember> GetOrgMemberAsync(Guid userId, Guid orgId);

    /// <summary>
    /// Returns active members of an org with User + AvatarFile loaded.
    /// Optionally filters by name/username search term and/or role.
    /// Caller applies pagination via PaginationExtensions.
    /// </summary>
    IQueryable<OrganizationMember> GetMembersQuery(Guid orgId, string? search = null, OrganizationRoleEnum? role = null);

    /// <summary>
    /// Returns a single active membership by its Id, including User and AvatarFile.
    /// </summary>
    IQueryable<OrganizationMember> GetMemberById(Guid membershipId);

    /// <summary>
    /// Returns all active org memberships for a user, including the Organization and its LogoFile.
    /// </summary>
    IQueryable<OrganizationMember> GetMembershipsByUserId(Guid userId);
}