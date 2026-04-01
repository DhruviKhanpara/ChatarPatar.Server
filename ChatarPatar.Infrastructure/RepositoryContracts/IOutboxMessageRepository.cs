using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOutboxMessageRepository
{
    Task AddAsync(OutboxMessage message);
    Task AddRangeAsync(List<OutboxMessage> messages);
    Task<List<OutboxMessage>> GetUnprocessedAsync();
    void Update(OutboxMessage message);
}
