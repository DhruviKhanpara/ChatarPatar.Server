using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;

public interface IOutboxMessageHandler
{
    string MessageType { get; }
    Task HandleAsync(OutboxMessage message);
}
