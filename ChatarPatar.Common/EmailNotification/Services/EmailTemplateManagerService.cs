using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.EmailNotification.Model;

namespace ChatarPatar.Common.EmailNotification.Services;

internal class EmailTemplateManagerService : IEmailTemplateManagerService
{
    public string GenerateEmailTemplate(EmailNotificationTemplate emailNotificationTemplate)
    {
        if (string.IsNullOrWhiteSpace(emailNotificationTemplate.TemplateString))
            throw new AppException("Unable to Retrieve email template");

        emailNotificationTemplate.TemplateStringReplacement
            .ToList()
            .ForEach(replacementString =>
            {
                emailNotificationTemplate.TemplateString = emailNotificationTemplate.TemplateString.Replace(replacementString.Key, replacementString.Value);
            });

        return emailNotificationTemplate.TemplateString;
    }
}
