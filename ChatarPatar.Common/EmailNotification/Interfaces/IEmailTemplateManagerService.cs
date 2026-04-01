using ChatarPatar.Common.EmailNotification.Model;

namespace ChatarPatar.Common.EmailNotification.Interfaces;

public interface IEmailTemplateManagerService
{
    string GenerateEmailTemplate(EmailNotificationTemplate emailNotificationTemplate);
}
