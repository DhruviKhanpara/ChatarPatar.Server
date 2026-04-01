using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ChatarPatar.Application.Services.Notification.Dispatcher;

internal class OutboxEmailDispatcher : INotificationDispatcher
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly HttpContext? _httpContext;
    private readonly IOutboxBackgroundQueue _queue;

    public OutboxEmailDispatcher(IRepositoryManager repositoryManager, IHttpContextAccessor httpContextAccessor, IOutboxBackgroundQueue queue)
    {
        _repositoryManager = repositoryManager;
        _httpContext = httpContextAccessor.HttpContext;
        _queue = queue;
    }

    public async Task DispatchAsync(NotificationPayload payload)
    {
        var message = new OutboxMessage
        {
            Type = payload.GetType().Name,
            Payload = JsonConvert.SerializeObject(payload),
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.TryParse(_httpContext!.GetUserId(), out Guid userId) ? userId : null,
            IsDeleted = false
        };

        await _repositoryManager.OutboxMessageRepository.AddAsync(message);
        await _repositoryManager.UnitOfWork.SaveChangesAsync();

        _queue.Enqueue();
    }

    public async Task DispatchManyAsync(List<NotificationPayload> payload)
    {
        List<OutboxMessage> messages = new List<OutboxMessage>();

        foreach (var item in payload)
        {
            messages.Add(new OutboxMessage
            {
                Type = item.GetType().Name,
                Payload = JsonConvert.SerializeObject(item),
                IsProcessed = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.TryParse(_httpContext!.GetUserId(), out Guid userId) ? userId : null,
                IsDeleted = false
            });
        }

        await _repositoryManager.OutboxMessageRepository.AddRangeAsync(messages);
        await _repositoryManager.UnitOfWork.SaveChangesAsync();

        _queue.Enqueue();
    }
}
