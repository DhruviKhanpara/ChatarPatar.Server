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

    IChannelService ChannelService { get; }
    IChannelMemberService ChannelMemberService { get; }

    IConversationService ConversationService { get; }
    IConversationParticipantService ConversationParticipantService { get; }

    IMessageService MessageService { get; }

    IFileService FileService { get; }

    IPermissionService PermissionService { get; }
}
