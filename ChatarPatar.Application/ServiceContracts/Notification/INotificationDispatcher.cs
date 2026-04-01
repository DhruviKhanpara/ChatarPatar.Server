using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts.Notification;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationPayload payload);
    Task DispatchManyAsync(List<NotificationPayload> payload);
}
