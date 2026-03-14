using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageMentionRepository : BaseRepository<MessageMention>, IMessageMentionRepository
{
    public MessageMentionRepository(AppDbContext context) : base(context) { }
}
