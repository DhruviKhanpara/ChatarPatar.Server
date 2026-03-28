using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Application.RepositoryContracts;

public interface IRepositoryManager
{
    IUnitOfWork UnitOfWork { get; }

    IUserRepository UserRepository { get; }
    IUserStatusRepository UserStatusRepository { get; }

    IOrganizationRepository OrganizationRepository { get; }
    IOrganizationMemberRepository OrganizationMemberRepository { get; }
    IOrganizationInviteRepository OrganizationInviteRepository { get; }

    ITeamRepository TeamRepository { get; }
    ITeamMemberRepository TeamMemberRepository { get; }

    IChannelRepository ChannelRepository { get; }
    IChannelMemberRepository ChannelMemberRepository { get; }

    IConversationRepository ConversationRepository { get; }
    IConversationParticipantRepository ConversationParticipantRepository { get; }

    IMessageRepository MessageRepository { get; }
    IPinnedMessageRepository PinnedMessageRepository { get; }
    IMessageReactionRepository MessageReactionRepository { get; }
    IMessageMentionRepository MessageMentionRepository { get; }
    IMessageAttachmentRepository MessageAttachmentRepository { get; }
    IMessageReceiptRepository MessageReceiptRepository { get; }

    IReadStateRepository ReadStateRepository { get; }

    INotificationRepository NotificationRepository { get; }

    IFileRepository FileRepository { get; }

    IRefreshTokenRepository RefreshTokenRepository { get; }
}
