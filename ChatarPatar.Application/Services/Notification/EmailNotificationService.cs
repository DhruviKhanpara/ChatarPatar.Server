using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.EmailNotification.Model;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.Extensions.Configuration;

namespace ChatarPatar.Application.Services.Notification;

internal class EmailNotificationService : IEmailNotificationService
{
    private readonly string _appName;
    private const string _baseUrl = "https://localhost:3000";

    private readonly IRepositoryManager _repositories;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IEmailTemplateManagerService _templateManager;

    public EmailNotificationService(IRepositoryManager repositories, INotificationDispatcher dispatcher, IEmailTemplateManagerService templateManager, IConfiguration config)
    {
        _repositories = repositories;
        _dispatcher = dispatcher;
        _templateManager = templateManager;
        _appName = config.GetValue<string>("AppSettings:ApplicationName") ?? "Unknown";
    }

    public async Task SendOrgInviteAsync(string toEmail, string orgName, string inviterName, string roleName, string inviteToken, int expiryDays)
    {
        var replacements = new Dictionary<string, string>
        {
            { "{{appName}}",        _appName },
            { "{{orgName}}",        orgName },
            { "{{inviterName}}",    inviterName },
            { "{{roleName}}",       roleName },
            { "{{inviteLink}}",     $"{_baseUrl}/auth/register?Token={inviteToken}" },
            { "{{recipientEmail}}", toEmail },
            { "{{expiryDays}}",     expiryDays.ToString() },
            { "{{year}}",           DateTime.UtcNow.Year.ToString() },
            { "{{orgInitial}}",     orgName.Length > 0 ? orgName[0].ToString().ToUpper() : "?" },
            { "{{privacyPolicy}}",  $"{_baseUrl}/privacy" }
        };

        var template = await RetrieveTemplate(templateName: NotificationTemplateNames.OrganizationInvite);

        if (template.SubjectText == null)
            throw new NotFoundAppException($"Subject not found in Email template.");

        var subject = GenerateEmailTemplate(template.SubjectText, replacements);
        var body = GenerateEmailTemplate(template.BodyText, replacements);

        await SendAsync(emailBody: body, subject: subject, toAddresses: new List<string> { toEmail }, null, null);
    }

    public async Task SendForgotPasswordOtpAsync(string toEmail, string userName, string otp, double expiryMinutes)
    {
        var replacements = new Dictionary<string, string>
        {
            { "{{appName}}",       _appName },
            { "{{userName}}",      userName },
            { "{{otp}}",           otp },
            { "{{expiryMinutes}}", expiryMinutes.ToString() },
            { "{{resetLink}}",     $"{_baseUrl}/auth/reset-password" },
            { "{{year}}",          DateTime.UtcNow.Year.ToString() },
            { "{{privacyPolicy}}", $"{_baseUrl}/privacy" }
        };

        var template = await RetrieveTemplate(templateName: NotificationTemplateNames.ForgotPassword);

        if (template.SubjectText == null)
            throw new NotFoundAppException("Subject not found in Email template.");

        var subject = GenerateEmailTemplate(template.SubjectText, replacements);
        var body = GenerateEmailTemplate(template.BodyText, replacements);

        await SendAsync(emailBody: body, subject: subject, toAddresses: new List<string> { toEmail }, null, null);
    }

    public async Task SendPasswordChangedAlertAsync(string toEmail, string userName, string device, string location)
    {
        var replacements = new Dictionary<string, string>
        {
            { "{{appName}}",       _appName },
            { "{{userName}}",      userName },
            { "{{dateTime}}",      DateTime.UtcNow.ToString("f") },
            { "{{device}}",        device },
            { "{{location}}",      location },
            { "{{securityLink}}",  $"{_baseUrl}/auth/security-settings" },
            { "{{year}}",          DateTime.UtcNow.Year.ToString() },
            { "{{privacyPolicy}}", $"{_baseUrl}/privacy" }
        };

        var template = await RetrieveTemplate(templateName: NotificationTemplateNames.PasswordChangedAlert);

        if (template.SubjectText == null)
            throw new NotFoundAppException("Subject not found in Email template.");

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
