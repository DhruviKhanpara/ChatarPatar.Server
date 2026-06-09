using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChatarPatar.Infrastructure.ExternalServices.Notification.Handlers;

internal class CloudinaryDeleteOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => CloudinaryDeletePayload.OutboxType;

    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<CloudinaryDeleteOutboxHandler> _logger;

    public CloudinaryDeleteOutboxHandler(ICloudinaryService cloudinaryService, ILogger<CloudinaryDeleteOutboxHandler> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task HandleAsync(OutboxMessage message)
    {
        var payload = JsonConvert.DeserializeObject<CloudinaryDeletePayload>(message.Payload)
            ?? throw new InvalidOperationException($"Could not deserialize payload for outbox message {message.Id}.");

        _logger.LogInformation("[OUTBOX] Cloudinary.Delete — PublicId={PublicId} FileType={FileType}", payload.PublicId, payload.FileType);

        await _cloudinaryService.DeleteFileAsync(payload.PublicId, payload.FileType);

        _logger.LogInformation("[OUTBOX] Cloudinary.Delete complete — PublicId={PublicId}", payload.PublicId);
    }
}
