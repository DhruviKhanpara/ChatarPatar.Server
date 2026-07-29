namespace ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;

public interface IOutboxProcessor
{
    Task<int> ProcessAsync(int batchSize, CancellationToken cancellationToken);
}
