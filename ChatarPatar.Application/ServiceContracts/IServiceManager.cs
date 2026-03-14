namespace ChatarPatar.Application.ServiceContracts;

public interface IServiceManager
{
    IUserService UserService { get; }

    IPermissionService PermissionService { get; }
}
