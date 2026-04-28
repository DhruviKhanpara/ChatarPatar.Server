namespace ChatarPatar.Infrastructure.RepositoryContracts;

/// <summary>
/// Encapsulates all bulk soft-delete / archive operations that cascade through
/// the org → team → channel hierarchy.
///
/// Every method issues the minimum number of UPDATE statements required via
/// ExecuteUpdateAsync — no entity rows are loaded into memory.
///
/// IMPORTANT: All methods must be called inside an open transaction.
///
/// Hierarchy:
///   CleanupChannelAsync          → archives channel + deletes its ChannelMembers (atomic pair)
///   CleanupTeamAsync             → deletes TeamMembers + calls CleanupChannelAsync per channel
///   BulkRemoveUser*Memberships   → org-level bulk deletes for the departing user
/// </summary>
public interface ICascadeCleanupRepository
{
    /// <summary>
    /// Level 1 — Channel cleanup (atomic pair).
    /// Archives the channel AND soft-deletes all its active ChannelMembers in two
    /// consecutive statements — no gap between them for a new member to slip in.
    /// Returns a <see cref="ChannelCleanupResult"/> with counts for audit.
    /// </summary>
    Task<ChannelCleanupResult> CleanupChannelAsync(Guid channelId, Guid actorId, DateTime now);

    /// <summary>
    /// Level 2 — Team cleanup.
    /// Executes in order:
    ///   1. Soft-deletes all active TeamMember rows for the team.
    ///   2. For all active channels in the team — CleanupChannelAsync (archive + delete members).
    ///      Uses a pure SQL JOIN predicate (no in-memory channel-id list) so new channels
    ///      added concurrently are also caught.
    /// Returns a <see cref="TeamCleanupResult"/> with per-table counts for audit.
    /// </summary>
    Task<TeamCleanupResult> CleanupTeamAsync(Guid teamId, Guid actorId, DateTime now);

    /// <summary>
    /// Soft-deletes all active TeamMember rows for a specific user across all teams in the org.
    /// Used for the bulk-remove step (Phase 3) when removing a user from an org.
    /// Returns row count for audit.
    ///
    /// IMPORTANT: Must run AFTER all targeted cleanup/promotion phases so that rows
    /// already soft-deleted by those phases are naturally skipped by the WHERE clause.
    /// </summary>
    Task<int> BulkRemoveUserTeamMembershipsAsync(Guid userId, Guid orgId, Guid actorId, DateTime now);

    /// <summary>
    /// Soft-deletes all active ChannelMember rows for a specific user across all channels in the org.
    /// Used for the bulk-remove step (Phase 3) when removing a user from an org.
    /// Returns row count for audit.
    ///
    /// IMPORTANT: Must run AFTER all targeted cleanup/promotion phases.
    /// </summary>
    Task<int> BulkRemoveUserChannelMembershipsAsync(Guid userId, Guid orgId, Guid actorId, DateTime now);

    /// <summary>
    /// Promotes a specific TeamMember row to TeamAdmin.
    /// Returns the number of rows affected (1 = success, 0 = row was concurrently deleted).
    /// The caller MUST check the return value and fall back to team cleanup on 0.
    /// </summary>
    Task<int> PromoteTeamMemberAsync(Guid membershipId, Guid actorId, DateTime now);

    /// <summary>
    /// Promotes a specific ChannelMember row to ChannelModerator.
    /// Returns the number of rows affected (1 = success, 0 = row was concurrently deleted).
    /// The caller MUST check the return value and fall back to channel cleanup on 0.
    /// </summary>
    Task<int> PromoteChannelMemberAsync(Guid membershipId, Guid actorId, DateTime now);

    /// <summary>
    /// Archives a single team record only — does NOT cascade to members or channels.
    /// Call CleanupTeamAsync first for the full cascade, then this to mark the team archived.
    /// </summary>
    Task ArchiveTeamAsync(Guid teamId, Guid actorId, DateTime now);
}

/// <summary>
/// Result of a full channel cleanup — counts for audit log summaries.
/// </summary>
public record ChannelCleanupResult(
    int ChannelMembersRemoved
);

/// <summary>
/// Result of a full team cleanup — counts for audit log summaries.
/// </summary>
public record TeamCleanupResult(
    int TeamMembersRemoved,
    int ChannelsArchived,
    int ChannelMembersRemoved
);