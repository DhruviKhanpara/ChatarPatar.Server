using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface INotificationRepository : IBaseRepository<NotificationEntity>
{
    IQueryable<NotificationEntity> GetForUserQuery(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<NotificationEntity?> GetByIdForUserAsync(Guid id, Guid userId);
    Task MarkAllAsReadAsync(Guid userId, DateTime readAt);
}
