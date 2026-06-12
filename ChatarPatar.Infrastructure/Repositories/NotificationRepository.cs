using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class NotificationRepository : BaseRepository<NotificationEntity>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public IQueryable<NotificationEntity> GetForUserQuery(Guid userId)
        => _context.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt);

    public Task<int> GetUnreadCountAsync(Guid userId)
        => _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);

    public Task<NotificationEntity?> GetByIdForUserAsync(Guid id, Guid userId)
        => _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == userId);

    public async Task MarkAllAsReadAsync(Guid userId, DateTime readAt)
        => await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, readAt));
}
