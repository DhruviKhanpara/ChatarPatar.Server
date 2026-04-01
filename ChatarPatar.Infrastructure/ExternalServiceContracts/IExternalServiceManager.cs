using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;

namespace ChatarPatar.Infrastructure.ExternalServiceContracts;

public interface IExternalServiceManager
{
    ICloudinaryService CloudinaryService { get; }

    IOutboxMessageHandler OutboxMessageHandler { get; }
    IOutboxProcessor OutboxProcessor { get; }
}
