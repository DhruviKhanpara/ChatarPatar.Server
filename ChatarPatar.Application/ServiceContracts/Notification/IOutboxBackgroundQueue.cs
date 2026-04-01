namespace ChatarPatar.Application.ServiceContracts.Notification;

public interface IOutboxBackgroundQueue
{
    void Enqueue();
    Task WaitAsync(CancellationToken cancellationToken);
}
