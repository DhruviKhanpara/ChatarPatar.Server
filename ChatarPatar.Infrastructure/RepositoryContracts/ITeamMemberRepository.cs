using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamMemberRepository : IBaseSoftDeleteRepository<TeamMember>
{
    /// <summary>
    /// Returns the active membership for a specific user in a team.
    /// </summary>
    IQueryable<TeamMember> GetTeamMemberAsync(Guid userId, Guid teamId);

    /// <summary>
    /// Returns a single active membership by its id within a specific team.
    /// Includes User and AvatarFile.
    /// </summary>
    IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId);

    /// <summary>
    /// Returns a queryable of all active TeamMember rows for a specific user
    /// across all teams that belong to the given org.
    /// </summary>
    IQueryable<TeamMember> GetAllActiveMembershipsForUserInOrgQuery(Guid userId, Guid orgId);

    /// <summary>
    /// Returns active members of a team with User + AvatarFile loaded.
    /// Optionally filters by name/username search and/or role.
    /// Caller applies pagination.
    /// </summary>
    IQueryable<TeamMember> GetMembersQuery(Guid teamId, string? search = null, TeamRoleEnum? role = null);

    /// <summary>
    /// Returns all active team memberships for a user within an org,
    /// including Team and its IconFile — used for sidebar listing.
    /// </summary>
    IQueryable<TeamMember> GetMembershipsByUserInOrg(Guid userId, Guid orgId);

    /// <summary>
    /// For each team in the org where userId is the ONLY active TeamAdmin,
    /// returns that team's id/name plus the UserId of the longest-standing
    /// other active member (null if no other members exist).
    /// Single query — no rows loaded for teams where the user is NOT sole admin.
    /// </summary>
    Task<List<SoleAdminTeamResult>> GetSoleAdminTeamsWithNextSeniorMemberAsync(Guid userId, Guid orgId);
}