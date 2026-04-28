using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IChannelMemberRepository : IBaseSoftDeleteRepository<ChannelMember>
{
    /// <summary>
    /// Returns the active membership for a specific user in a channel.
    /// </summary>
    IQueryable<ChannelMember> GetChannelMemberAsync(Guid userId, Guid channelId);

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
    Task<List<SoleModeratorChannelResult>> GetSoleModeratorPrivateChannelsWithNextSeniorMemberAsync(Guid userId, Guid orgId);
}