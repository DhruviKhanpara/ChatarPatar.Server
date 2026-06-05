using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IChannelMemberRepository : IBaseSoftDeleteRepository<ChannelMember>
{
    /// <summary>
    /// Returns a specific membership record by its id within a channel.
    /// </summary>
    IQueryable<ChannelMember> GetByIdInChannel(Guid membershipId, Guid channelId);

    /// <summary>
    /// For each PRIVATE channel in the org where <paramref name="userId"/> is the ONLY
    /// active ChannelModerator, returns that channel + the membership id and user id of
    /// the longest-standing other active member (null when the channel would become empty).
    /// Also returns the total active member count so the caller can detect empty channels.
    ///
    /// Public channels are excluded — they have no ChannelMember rows.
    /// Single round-trip.
    /// </summary>
    Task<List<SoleModeratorChannelResult>> GetSoleModeratorPrivateChannelsInOrgWithNextSeniorMemberAsync(Guid userId, Guid orgId);

    /// <summary>
    /// Same as the org-scoped overload but filtered to a single team.
    /// Used during team member remove/leave to only resolve channels within that team.
    /// </summary>
    Task<List<SoleModeratorChannelResult>> GetSoleModeratorPrivateChannelsInTeamWithNextSeniorMemberAsync(Guid userId, Guid teamId);
}