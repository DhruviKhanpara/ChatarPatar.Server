using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatarPatar.Infrastructure.ExternalServices.Notification.Processor;

internal class GenericOutboxProcessor : IOutboxProcessor
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IEnumerable<IOutboxMessageHandler> _handlers;
    private readonly ILogger<GenericOutboxProcessor> _logger;
    private readonly OutboxRetrySettings _retrySettings;

    public GenericOutboxProcessor(IRepositoryManager repositoryManager, IEnumerable<IOutboxMessageHandler> handlers, ILogger<GenericOutboxProcessor> logger, IOptions<OutboxRetrySettings> retrySettings)
    {
        _repositoryManager = repositoryManager;
        _handlers = handlers;
        _logger = logger;
        _retrySettings = retrySettings.Value;
    }

    public async Task ProcessAsync()
    {
        var messages = await _repositoryManager.OutboxMessageRepository.GetUnprocessedAsync();

        foreach (var message in messages)
        {
            var handler = _handlers.FirstOrDefault(x => x.MessageType == message.Type);
            if (handler == null)
            {
                _logger.LogWarning($"[OUTBOX] No handler for type {message.Type}");
                continue;
            }

            try
            {
                await handler.HandleAsync(message);
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                if (message.RetryCount >= _retrySettings.RetryCount)
                {
                    message.IsProcessed = true;
                    message.ProcessedAt = DateTime.UtcNow;
                    _logger.LogWarning($"[OUTBOX] Message {message.Type} failed after max retries. Marking as processed.");
                }
                else
                {
                    message.RetryCount++;
                    message.NextAttemptAt = DateTime.UtcNow.Add(
                        RetryHelper.GetExponentialBackoff(message.RetryCount, _retrySettings.RetryDelayMinutes)
                    );
                }

                _logger.LogError($"[OUTBOX] Error processing message. Type={message.Type}, Error={ex.Message}");
            }

            _repositoryManager.OutboxMessageRepository.Update(message);
        }

        await _repositoryManager.UnitOfWork.SaveChangesAsync();
    }
}
