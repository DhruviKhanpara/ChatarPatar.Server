using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IChannelMemberRepository : IBaseSoftDeleteRepository<ChannelMember>
{
    IQueryable<ChannelMember> GetChannelMemberAsync(Guid userId, Guid channelId);
}