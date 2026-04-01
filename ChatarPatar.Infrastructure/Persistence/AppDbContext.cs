using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Entities
    public virtual DbSet<Channel> Channels { get; set; }
    public virtual DbSet<ChannelMember> ChannelMembers { get; set; }
    public virtual DbSet<Conversation> Conversations { get; set; }
    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public virtual DbSet<FileEntity> Files { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<MessageAttachment> MessagesAttachments { get; set; }
    public virtual DbSet<MessageMention> MessagesMentions { get; set; }
    public virtual DbSet<MessageReaction> MessagesReactions { get; set; }
    public virtual DbSet<MessageReceipt> MessagesReceipts { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Organization> Organizations { get; set; }
    public virtual DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public virtual DbSet<OrganizationInvite> OrganizationInvites { get; set; }
    public virtual DbSet<PinnedMessage> PinnedMessages { get; set; }
    public virtual DbSet<ReadState> ReadStates { get; set; }
    public virtual DbSet<Team> Teams { get; set; }
    public virtual DbSet<TeamMember> TeamMembers { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserStatus> UsersStatus { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }
    public virtual DbSet<NotificationTemplate> NotificationTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
