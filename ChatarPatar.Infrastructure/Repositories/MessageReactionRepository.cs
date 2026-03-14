using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageReactionRepository : BaseRepository<MessageReaction>, IMessageReactionRepository
{
    public MessageReactionRepository(AppDbContext context) : base(context) { }
}
