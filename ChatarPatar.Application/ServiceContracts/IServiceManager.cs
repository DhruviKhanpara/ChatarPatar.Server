namespace ChatarPatar.Application.ServiceContracts;

public interface IServiceManager
{
    IUserService UserService { get; }
    IOrganizationService OrganizationService { get; }
    IOrganizationInviteService OrganizationInviteService { get; }
    IOrganizationMemberService OrganizationMemberService { get; }

    IPermissionService PermissionService { get; }
}
