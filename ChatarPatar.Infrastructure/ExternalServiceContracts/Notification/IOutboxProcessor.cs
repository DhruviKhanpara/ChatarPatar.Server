namespace ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;

public interface IOutboxProcessor
{
    Task ProcessAsync();
}
