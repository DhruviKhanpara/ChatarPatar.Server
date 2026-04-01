using ChatarPatar.Common.EmailNotification.Model;

namespace ChatarPatar.Common.EmailNotification.Interfaces;

public interface IEmailHandlerService
{
    Task SendAsync(EmailNotificationRequest request);
}
