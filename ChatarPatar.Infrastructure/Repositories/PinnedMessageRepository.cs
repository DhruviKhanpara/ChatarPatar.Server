using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class PinnedMessageRepository : BaseRepository<PinnedMessage>, IPinnedMessageRepository
{
    public PinnedMessageRepository(AppDbContext context) : base(context) { }
}
