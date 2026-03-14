using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageAttachmentRepository : BaseRepository<MessageAttachment>, IMessageAttachmentRepository
{
    public MessageAttachmentRepository(AppDbContext context) : base(context) { }
}
