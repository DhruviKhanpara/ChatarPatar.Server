using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class MessageReceiptRepository : BaseRepository<MessageReceipt>, IMessageReceiptRepository
{
    public MessageReceiptRepository(AppDbContext context) : base(context) { }
}
