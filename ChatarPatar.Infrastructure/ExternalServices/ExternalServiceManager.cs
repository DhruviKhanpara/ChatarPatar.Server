using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Infrastructure.ExternalServices;

internal sealed class ExternalServiceManager : IExternalServiceManager
{
    private readonly IServiceProvider _provider;

    public ExternalServiceManager(IServiceProvider provider)
    {
        _provider = provider;
    }

    private T Get<T>() where T : class => _provider.GetRequiredService<T>();

    public ICloudinaryService CloudinaryService => Get<ICloudinaryService>();

    public IOutboxMessageHandler OutboxMessageHandler => Get<IOutboxMessageHandler>();
    public IOutboxProcessor OutboxProcessor => Get<IOutboxProcessor>();
}
