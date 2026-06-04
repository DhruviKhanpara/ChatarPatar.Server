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
/// NextSeniorMemberId may be null when the departing user is the only member of
/// the private channel — in that case the channel is archived with no promotion.
/// never return those channels.
/// </remarks>
public record SoleModeratorChannelResult(
    Guid ChannelId,
    string ChannelName,
    Guid TeamId,
    Guid? NextSeniorMemberId,
    Guid? NextSeniorMembershipId
);