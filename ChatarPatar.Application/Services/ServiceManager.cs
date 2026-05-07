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

    public IAuthService AuthService => Get<IAuthService>();
    public IUserService UserService => Get<IUserService>();
    public IOrganizationService OrganizationService => Get<IOrganizationService>();
    public IOrganizationInviteService OrganizationInviteService => Get<IOrganizationInviteService>();
    public IOrganizationMemberService OrganizationMemberService => Get<IOrganizationMemberService>();

    public ITeamService TeamService => Get<ITeamService>();
    public ITeamMemberService TeamMemberService => Get<ITeamMemberService>();

    public IPermissionService PermissionService => Get<IPermissionService>();
}
