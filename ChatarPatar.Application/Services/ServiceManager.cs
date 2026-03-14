using ChatarPatar.Application.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Application.Services;

internal sealed class ServiceManager : IServiceManager
{
    private readonly IServiceProvider _provider;

    public ServiceManager(IServiceProvider provider)
    {
        _provider = provider;
    }

    private T Get<T>() where T : class => _provider.GetRequiredService<T>();

    public IUserService UserService => Get<IUserService>();

    public IPermissionService PermissionService => Get<IPermissionService>();
}
