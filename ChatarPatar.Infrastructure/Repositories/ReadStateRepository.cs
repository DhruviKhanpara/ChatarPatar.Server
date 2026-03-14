using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ReadStateRepository : BaseRepository<ReadState>, IReadStateRepository
{
    public ReadStateRepository(AppDbContext context) : base(context) { }
}
