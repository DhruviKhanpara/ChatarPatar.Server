using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services.Notification.BackgroundServices;

internal class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxBackgroundQueue _queue;
    private readonly ILogger<OutboxBackgroundService> _logger;

    // Fallback poll — catches anything missed if a signal was dropped
    private static readonly TimeSpan FallbackInterval = TimeSpan.FromMinutes(1);

    public OutboxBackgroundService(IServiceProvider serviceProvider, IOutboxBackgroundQueue queue, ILogger<OutboxBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a signal OR fallback timeout — whichever comes first
                using var timeoutCts = new CancellationTokenSource(FallbackInterval);
                using var linkedCts = CancellationTokenSource
                    .CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

                try
                {
                    await _queue.WaitAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Timeout expired — that's fine, just run the processor anyway
                }

                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App is shutting down — exit cleanly
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OUTBOX] Background service error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
            await processor.ProcessAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OUTBOX] Processor error");
        }
    }
}
