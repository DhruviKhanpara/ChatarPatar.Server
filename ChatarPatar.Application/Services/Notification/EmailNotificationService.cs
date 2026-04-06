using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.EmailNotification.Model;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Application.Services.Notification;

internal class EmailNotificationService : IEmailNotificationService
{
    private readonly IRepositoryManager _repositories;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IEmailTemplateManagerService _templateManager;

    public EmailNotificationService(
        IRepositoryManager repositories,
        INotificationDispatcher dispatcher,
        IEmailTemplateManagerService templateManager)
    {
        _repositories = repositories;
        _dispatcher = dispatcher;
        _templateManager = templateManager;
    }

    public async Task SendOrgInviteAsync(string toEmail, string orgName, string inviterName, string roleName, string inviteToken, int expiryDays)
    {
        var replacements = new Dictionary<string, string>
        {
            { "{{orgName}}",        orgName },
            { "{{inviterName}}",    inviterName },
            { "{{roleName}}",       roleName },
            { "{{inviteLink}}",     $"https://localhost:3000/Auth/register?Token={inviteToken}" },
            { "{{recipientEmail}}", toEmail },
            { "{{expiryDays}}",     expiryDays.ToString() },
            { "{{year}}",           DateTime.UtcNow.Year.ToString() },
            { "{{orgInitial}}",     orgName.Length > 0 ? orgName[0].ToString().ToUpper() : "?" },
        };

        var template = await RetrieveTemplate(templateName: NotificationTemplateNames.OrganizationInvite);

        if (template.SubjectText == null)
            throw new NotFoundAppException($"Subject not found in Email template.");

        var subject = GenerateEmailTemplate(template.SubjectText, replacements);
        var body = GenerateEmailTemplate(template.BodyText, replacements);

        await SendAsync(emailBody: body, subject: subject, toAddresses: new List<string> { toEmail }, null, null);
    }

    #region Private section

    private async Task<NotificationTemplate> RetrieveTemplate(string templateName)
    {
        var template = await _repositories.NotificationTemplateRepository
            .GetByNameAndTypeAsync(templateName, NotificationTemplateTypeEnum.Email);

        return template;
    }

    private string GenerateEmailTemplate(string text, Dictionary<string, string> replacements)
    {
        var emailNotificationTemplate = new EmailNotificationTemplate(text, replacements);
        return _templateManager.GenerateEmailTemplate(emailNotificationTemplate);
    }

    private async Task SendAsync(string emailBody, string subject, List<string> toAddresses, List<string>? cCAddresses, List<string>? bCCAddresses, string? fromAddress = null)
    {
        var request = new EmailNotificationRequest(
            emailBody: emailBody,
            subject: subject,
            toAddresses: toAddresses,
            cCAddresses: cCAddresses,
            bCCAddresses: bCCAddresses,
            fromAddress: fromAddress
        );

        await _dispatcher.DispatchAsync(new EmailPayload(request));
    }

    #endregion
}
