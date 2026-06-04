namespace ChatarPatar.Infrastructure.RepositoryContracts;

/// <summary>
/// Encapsulates all bulk soft-delete / archive operations that cascade through
/// the org → team → channel hierarchy.
///
/// ExecuteUpdateAsync — no entity rows are loaded into memory.
///
/// IMPORTANT: All methods must be called inside an open transaction.
///
/// Hierarchy:
///   ArchiveChannelAsync          → archives channel + preserved its ChannelMembers (atomic pair)
///   ArchiveTeamAsync             → calls ArchiveChannelAsync per channel
///   BulkRemoveUser*Memberships   → org-level bulk deletes for the departing user
/// </summary>
public interface ICascadeCleanupRepository
{
    /// <summary>
    /// Archives a channel.
    /// Memberships are preserved — consistent with the normal archive-channel endpoint.
    /// </summary>
    Task ArchiveChannelAsync(Guid channelId, Guid actorId, DateTime now);

    /// <summary>
    /// Archives a team and all its active channels.
    /// Memberships are preserved — consistent with the normal archive-team endpoint.
    /// </summary>
    /// <returns>Related channel Archive count</returns>
    Task<int> ArchiveTeamAsync(Guid teamId, Guid actorId, DateTime now);

    /// <summary>
    /// Soft-deletes all active TeamMember rows for a user across an org.
    /// </summary>
    Task<int> BulkRemoveUserTeamMembershipsAsync(Guid userId, Guid orgId, Guid actorId, DateTime now);

    /// <summary>
    /// Soft-deletes all active ChannelMember rows for a user across an org.
    /// </summary>
    Task<int> RemoveUserChannelMembershipsInOrgAsync(Guid userId, Guid orgId, Guid actorId, DateTime now);

    /// <summary>
    /// Promotes a TeamMember row to TeamAdmin.
    /// Returns rows affected — 0 means the row was concurrently deleted.
    /// </summary>
    Task<int> PromoteTeamMemberAsync(Guid membershipId, Guid actorId, DateTime now);

    /// <summary>
    /// Promotes a ChannelMember row to ChannelModerator.
    /// Returns rows affected — 0 means the row was concurrently deleted.
    /// </summary>
    Task<int> PromoteChannelMemberAsync(Guid membershipId, Guid actorId, DateTime now);
}