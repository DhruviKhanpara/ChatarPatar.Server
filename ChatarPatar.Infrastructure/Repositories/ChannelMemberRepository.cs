using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ChannelMemberRepository : BaseSoftDeleteRepository<ChannelMember>, IChannelMemberRepository
{
    public ChannelMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<ChannelMember> GetChannelMemberAsync(Guid userId, Guid channelId) => 
        FindByCondition(x => x.UserId == userId && x.ChannelId == channelId);

    public IQueryable<ChannelMember> GetByIdInChannel(Guid membershipId, Guid channelId) =>
        FindByCondition(x => x.Id == membershipId && x.ChannelId == channelId);
}