using ChatarPatar.Common.EmailNotification.Model;

namespace ChatarPatar.Common.Models;

public abstract class NotificationPayload { }

public class EmailPayload : NotificationPayload
{
    public EmailPayload() { }

    public EmailPayload(EmailNotificationRequest request)
    {
        Request = request;
    }

    public EmailNotificationRequest Request { get; set; } = null!;
}
