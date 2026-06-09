using ChatarPatar.Common.AppLogging.Model;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Context;

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
            
            var initiatedBy = ExtractInitiatedBy(message.Payload);
            if (string.IsNullOrWhiteSpace(initiatedBy))
                initiatedBy = null;

            using (LogContext.PushProperty(LoggingProperties.UserName, initiatedBy))
            {
                if (handler == null)
                {
                    _logger.LogError($"[OUTBOX] No handler for type {message.Type}");
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
                        _logger.LogWarning("[OUTBOX] Message {MessageType} failed after max retries. Marking as processed.", message.Type);
                    }
                    else
                    {
                        message.RetryCount++;
                        message.NextAttemptAt = DateTime.UtcNow.Add(
                            RetryHelper.GetExponentialBackoff(message.RetryCount, _retrySettings.RetryDelayMinutes)
                        );
                    }

                    _logger.LogError(ex, "[OUTBOX] Error processing message. Type={MessageType}", message.Type);
                }
                
                _repositoryManager.OutboxMessageRepository.Update(message);
            }
        }

        using (LogContext.PushProperty(LoggingProperties.UserName, "System"))
        {
            await _repositoryManager.UnitOfWork.SaveChangesAsync();
        }
    }

    private static string ExtractInitiatedBy(string payload)
    {
        try
        {
            var basePayload = JsonConvert.DeserializeAnonymousType(payload, new { InitiatedBy = (string?)null });
            return basePayload?.InitiatedBy ?? "System";
        }
        catch
        {
            return "System";
        }
    }
}
