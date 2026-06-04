using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Infrastructure.Repositories;

internal sealed class RepositoryManager : IRepositoryManager
{
    private readonly AppDbContext _context;
    private readonly ILoggerFactory _loggerFactory;

    private readonly Lazy<IUnitOfWork> _unitOfWork;

    private readonly Lazy<IUserRepository> _userRepository;
    private readonly Lazy<IUserStatusRepository> _userStatusRepository;

    private readonly Lazy<IOrganizationRepository> _organizationRepository;
    private readonly Lazy<IOrganizationMemberRepository> _organizationMemberRepository;
    private readonly Lazy<IOrganizationInviteRepository> _organizationInviteRepository;

    private readonly Lazy<ITeamRepository> _teamRepository;
    private readonly Lazy<ITeamMemberRepository> _teamMemberRepository;

    private readonly Lazy<IChannelRepository> _channelRepository;
    private readonly Lazy<IChannelMemberRepository> _channelMemberRepository;

    private readonly Lazy<IConversationRepository> _conversationRepository;
    private readonly Lazy<IConversationParticipantRepository> _conversationParticipantRepository;

    private readonly Lazy<IMessageRepository> _messageRepository;
    private readonly Lazy<IPinnedMessageRepository> _pinnedMessageRepository;
    private readonly Lazy<IMessageReactionRepository> _messageReactionRepository;
    private readonly Lazy<IMessageMentionRepository> _messageMentionRepository;
    private readonly Lazy<IMessageAttachmentRepository> _messageAttachmentRepository;
    private readonly Lazy<IMessageReceiptRepository> _messageReceiptRepository;

    private readonly Lazy<IReadStateRepository> _readStateRepository;

    private readonly Lazy<INotificationRepository> _notificationRepository;

    private readonly Lazy<IFileRepository> _fileRepository;

    private readonly Lazy<IRefreshTokenRepository> _refreshTokenRepository;

    private readonly Lazy<IOutboxMessageRepository> _outboxMessageRepository;

    private readonly Lazy<INotificationTemplateRepository> _notificationTemplateRepository;
    
    private readonly Lazy<IOtpVerificationRepository> _otpVerificationRepository;

    private readonly Lazy<ICascadeCleanupRepository> _cascadeCleanupRepository;

    public RepositoryManager(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
        _unitOfWork = new Lazy<IUnitOfWork>(() => new UnitOfWork(_context, httpContextAccessor, _loggerFactory.CreateLogger<UnitOfWork>()));
        _userRepository = new Lazy<IUserRepository>(() => new UserRepository(_context));
        _userStatusRepository = new Lazy<IUserStatusRepository>(() => new UserStatusRepository(_context));
        _organizationRepository = new Lazy<IOrganizationRepository>(() => new OrganizationRepository(_context));
        _organizationMemberRepository = new Lazy<IOrganizationMemberRepository>(() => new OrganizationMemberRepository(_context));
        _organizationInviteRepository = new Lazy<IOrganizationInviteRepository>(() => new OrganizationInviteRepository(_context));
        _teamRepository = new Lazy<ITeamRepository>(() => new TeamRepository(_context));
        _teamMemberRepository = new Lazy<ITeamMemberRepository>(() => new TeamMemberRepository(_context));
        _channelRepository = new Lazy<IChannelRepository>(() => new ChannelRepository(_context));
        _channelMemberRepository = new Lazy<IChannelMemberRepository>(() => new ChannelMemberRepository(_context));
        _conversationRepository = new Lazy<IConversationRepository>(() => new ConversationRepository(_context));
        _conversationParticipantRepository = new Lazy<IConversationParticipantRepository>(() => new ConversationParticipantRepository(_context));
        _messageRepository = new Lazy<IMessageRepository>(() => new MessageRepository(_context));
        _pinnedMessageRepository = new Lazy<IPinnedMessageRepository>(() => new PinnedMessageRepository(_context));
        _messageReactionRepository = new Lazy<IMessageReactionRepository>(() => new MessageReactionRepository(_context));
        _messageMentionRepository = new Lazy<IMessageMentionRepository>(() => new MessageMentionRepository(_context));
        _messageAttachmentRepository = new Lazy<IMessageAttachmentRepository>(() => new MessageAttachmentRepository(_context));
        _messageReceiptRepository = new Lazy<IMessageReceiptRepository>(() => new MessageReceiptRepository(_context));
        _readStateRepository = new Lazy<IReadStateRepository>(() => new ReadStateRepository(_context));
        _notificationRepository = new Lazy<INotificationRepository>(() => new NotificationRepository(_context));
        _fileRepository = new Lazy<IFileRepository>(() => new FileRepository(_context));
        _refreshTokenRepository = new Lazy<IRefreshTokenRepository>(() => new RefreshTokenRepository(_context));
        _outboxMessageRepository = new Lazy<IOutboxMessageRepository>(() => new OutboxMessageRepository(_context));
        _notificationTemplateRepository = new Lazy<INotificationTemplateRepository>(() => new NotificationTemplateRepository(_context));
        _otpVerificationRepository = new Lazy<IOtpVerificationRepository>(() => new OtpVerificationRepository(_context));
        _cascadeCleanupRepository = new Lazy<ICascadeCleanupRepository>(() => new CascadeCleanupRepository(_context));
    }

    public IUnitOfWork UnitOfWork => _unitOfWork.Value;

    public IUserRepository UserRepository => _userRepository.Value;
    public IUserStatusRepository UserStatusRepository => _userStatusRepository.Value;

    public IOrganizationRepository OrganizationRepository => _organizationRepository.Value;
    public IOrganizationMemberRepository OrganizationMemberRepository => _organizationMemberRepository.Value;
    public IOrganizationInviteRepository OrganizationInviteRepository => _organizationInviteRepository.Value;

    public ITeamRepository TeamRepository => _teamRepository.Value;
    public ITeamMemberRepository TeamMemberRepository => _teamMemberRepository.Value;

    public IChannelRepository ChannelRepository => _channelRepository.Value;
    public IChannelMemberRepository ChannelMemberRepository => _channelMemberRepository.Value;

    public IConversationRepository ConversationRepository => _conversationRepository.Value;
    public IConversationParticipantRepository ConversationParticipantRepository => _conversationParticipantRepository.Value;

    public IMessageRepository MessageRepository => _messageRepository.Value;
    public IPinnedMessageRepository PinnedMessageRepository => _pinnedMessageRepository.Value;
    public IMessageReactionRepository MessageReactionRepository => _messageReactionRepository.Value;
    public IMessageMentionRepository MessageMentionRepository => _messageMentionRepository.Value;
    public IMessageAttachmentRepository MessageAttachmentRepository => _messageAttachmentRepository.Value;
    public IMessageReceiptRepository MessageReceiptRepository => _messageReceiptRepository.Value;

    public IReadStateRepository ReadStateRepository => _readStateRepository.Value;

    public INotificationRepository NotificationRepository => _notificationRepository.Value;

    public IFileRepository FileRepository => _fileRepository.Value;

    public IRefreshTokenRepository RefreshTokenRepository => _refreshTokenRepository.Value;

    public IOutboxMessageRepository OutboxMessageRepository => _outboxMessageRepository.Value;

    public INotificationTemplateRepository NotificationTemplateRepository => _notificationTemplateRepository.Value;
    
    public IOtpVerificationRepository OtpVerificationRepository => _otpVerificationRepository.Value;

    public ICascadeCleanupRepository CascadeCleanupRepository => _cascadeCleanupRepository.Value;
}
