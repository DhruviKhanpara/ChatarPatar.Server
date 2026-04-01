using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using Newtonsoft.Json;

namespace ChatarPatar.Infrastructure.ExternalServices.Notification.Handlers;

internal class EmailOutboxMessageHandler : IOutboxMessageHandler
{
    private readonly IEmailHandlerService _emailHandler;

    public string MessageType => nameof(EmailPayload);

    public EmailOutboxMessageHandler(IEmailHandlerService emailHandler)
    {
        _emailHandler = emailHandler;
    }

    public async Task HandleAsync(OutboxMessage message)
    {
        var payload = JsonConvert.DeserializeObject<EmailPayload>(message.Payload);

        if (payload == null)
            throw new AppException("Email request not found");

        await _emailHandler.SendAsync(payload.Request);
    }
}
