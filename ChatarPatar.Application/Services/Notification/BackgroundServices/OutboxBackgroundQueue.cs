using ChatarPatar.Application.ServiceContracts.Notification;
using System.Threading.Channels;

namespace ChatarPatar.Application.Services.Notification.BackgroundServices;

internal class OutboxBackgroundQueue : IOutboxBackgroundQueue
{
    // Capacity 100 — if somehow 100 signals pile up, 
    // extras are dropped (BoundedChannelFullMode.DropOldest)
    // because one signal is enough to trigger a full DB sweep anyway
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
}
