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
}