using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ChannelRepository : BaseSoftDeleteRepository<Channel>, IChannelRepository
{
    public ChannelRepository(AppDbContext context) : base(context) { }
}

