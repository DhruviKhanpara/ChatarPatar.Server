using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Notification;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(PaginationParams paginationParams);
    Task<UnreadCountDto> GetUnreadCountAsync();
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync();
}
