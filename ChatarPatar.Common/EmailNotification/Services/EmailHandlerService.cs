using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.AppLogging.Extensions.LoggerExtensions;
using ChatarPatar.Common.AppLogging.Model;
using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.EmailNotification.Model;
using ChatarPatar.Common.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ChatarPatar.Common.EmailNotification.Services;

internal class EmailHandlerService : IEmailHandlerService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailHandlerService> _logger;

    public EmailHandlerService(IOptions<EmailSettings> options, ILogger<EmailHandlerService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailNotificationRequest request)
    {
        // Redirect recipients if config says so
        if (!_settings.SendToActualRecipients)
        {
            if (string.IsNullOrWhiteSpace(_settings.OverrideEmail))
                throw new InvalidOperationException("Redirect email must be set when SendToActualRecipients is false.");

            request.ToAddresses = _settings.OverrideEmail.Split(',').Select(email => email.Trim()).ToList();
            request.CCAddresses = new List<string>();
            request.BCCAddresses = new List<string>();
        }

        request.FromAddress = _settings.From;
        request.DisplayName = _settings.DisplayName;

        EmailNotificationResult emailNotificationResult = new EmailNotificationResult(request, DateTime.UtcNow, null, null);

        try
        {
            var message = GenerateMessage(request);
            var response = await SendMailAsync(message: message);

            if (response is not null)
                throw new AppException(response);

            emailNotificationResult = new EmailNotificationResult(request, DateTime.UtcNow, DateTime.UtcNow, response);
        }
        catch (Exception ex)
        {
            emailNotificationResult = new EmailNotificationResult(request, DateTime.UtcNow, null, $"{ex.Message} {(ex.InnerException != null ? ex.InnerException : "")}");
        }

        _logger.WriteCommunication_Email(
            emailNotificationResult.ErrorMessage is null ? DeliveryStatus.Success : DeliveryStatus.Failure,
            request.FromAddress,
            request.ToAddresses,
            request.CCAddresses,
            request.BCCAddresses,
            request.Subject,
            request.EmailBody,
            emailNotificationResult.ErrorMessage
        );
    }

    #region Private Section

    private MimeMessage GenerateMessage(EmailNotificationRequest request)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.DisplayName, request.FromAddress));
        message.Subject = request.Subject;

        var builder = new BodyBuilder { HtmlBody = request.EmailBody };
        message.Body = builder.ToMessageBody();

        message.To.AddRange(request.ToAddresses.Select(MailboxAddress.Parse));

        if (request.CCAddresses != null && request.CCAddresses.Any())
            message.Cc.AddRange(request.CCAddresses.Select(MailboxAddress.Parse));

        if (request.BCCAddresses != null && request.BCCAddresses.Any())
            message.Bcc.AddRange(request.BCCAddresses.Select(MailboxAddress.Parse));

        return message;
    }

    private async Task<string?> SendMailAsync(MimeMessage message)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.Message} {(ex.InnerException != null ? ex.InnerException : "")}";
        }
    }

    #endregion
}
