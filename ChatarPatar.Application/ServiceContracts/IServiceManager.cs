namespace ChatarPatar.Application.ServiceContracts;

public interface IServiceManager
{
    IAuthService AuthService { get; }
    IUserService UserService { get; }
    IOrganizationService OrganizationService { get; }
    IOrganizationInviteService OrganizationInviteService { get; }
    IOrganizationMemberService OrganizationMemberService { get; }

    ITeamService TeamService { get; }
    ITeamMemberService TeamMemberService { get; }

    IPermissionService PermissionService { get; }
}
