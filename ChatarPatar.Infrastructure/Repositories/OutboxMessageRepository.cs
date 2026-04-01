using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly AppDbContext _context;

    public OutboxMessageRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(OutboxMessage message)
        => await _context.OutboxMessages.AddAsync(message);

    public async Task AddRangeAsync(List<OutboxMessage> messages)
        => await _context.OutboxMessages.AddRangeAsync(messages);

    public async Task<List<OutboxMessage>> GetUnprocessedAsync()
        => await _context.OutboxMessages
            .Where(x => !x.IsProcessed && (x.NextAttemptAt == null || x.NextAttemptAt <= DateTime.UtcNow))
            .ToListAsync();

    public void Update(OutboxMessage message)
            => _context.OutboxMessages.Update(message);
}
