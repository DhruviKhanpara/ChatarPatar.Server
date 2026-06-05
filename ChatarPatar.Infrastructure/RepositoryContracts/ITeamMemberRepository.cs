using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamMemberRepository : IBaseSoftDeleteRepository<TeamMember>
{

    /// <summary>
    /// Returns a single active membership by its id within a specific team.
    /// Includes User and AvatarFile.
    /// </summary>
    IQueryable<TeamMember> GetByIdInTeam(Guid membershipId, Guid teamId);

    /// <summary>
    /// Returns active members of a team with User + AvatarFile loaded.
    /// Optionally filters by name/username search and/or role.
    /// Caller applies pagination.
    /// </summary>
    IQueryable<TeamMember> GetTeamMembersQuery(Guid teamId, string? search = null, TeamRoleEnum? role = null);

    /// <summary>
    /// Fetches a single active membership row with an UPDLOCK + HOLDLOCK hint.
    ///
    /// Must be called inside an open transaction. The lock is held until the
    /// transaction commits or rolls back, guaranteeing that exactly one concurrent
    /// execution proceeds through the cascade phases while others either wait or
    /// find the row already soft-deleted and bail out.
    ///
    /// Returns null when the row does not exist or is already soft-deleted.
    /// </summary>
    Task<TeamMember?> GetByIdWithUpdateLockAsync(Guid membershipId);

    /// <summary>
    /// For each team in the org where userId is the ONLY active TeamAdmin,
    /// returns that team's id/name plus the UserId of the longest-standing
    /// other active member (null if no other members exist).
    /// Single query — no rows loaded for teams where the user is NOT sole admin.
    /// </summary>
    Task<List<SoleAdminTeamResult>> GetSoleAdminTeamsWithNextSeniorMemberAsync(Guid userId, Guid orgId);
}