namespace ChatarPatar.Common.Models;

/// <summary>
/// Returned by GetSoleAdminTeamsWithNextSeniorMemberAsync.
/// Represents one team where the departing user is the only TeamAdmin.
/// </summary>
public record SoleAdminTeamResult(
    Guid TeamId,
    string TeamName,

    /// <summary>
    /// UserId of the longest-standing other active member.
    /// Null when no other members exist — team should be archived.
    /// </summary>
    Guid? NextSeniorMemberId,
    Guid? NextSeniorMembershipId);

/// <summary>
/// Returned by GetSoleModeratorPrivateChannelsWithNextSeniorMemberAsync.
/// Represents one private channel where the departing user is the only ChannelModerator.
/// </summary>
/// <remarks>
/// TotalMemberCount is intentionally absent — an empty-channel branch is unreachable here.
///
/// If the team survived Phase 1 (was NOT archived), other team members exist, and for a
/// private channel those members must have an explicit ChannelMember row. So
/// NextSeniorMemberId is always non-null by the time Phase 2 runs.
///
/// If the team WAS archived in Phase 1 (truly no other members), all its channels were
/// archived and their ChannelMember rows removed in that same phase — this query will
/// never return those channels.
/// </remarks>
public record SoleModeratorChannelResult(
    Guid ChannelId,
    string ChannelName,
    Guid TeamId,
    Guid? NextSeniorMemberId,
    Guid? NextSeniorMembershipId
);