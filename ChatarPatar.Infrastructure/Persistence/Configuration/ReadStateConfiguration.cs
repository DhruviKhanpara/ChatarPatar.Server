using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class ReadStateConfiguration : IEntityTypeConfiguration<ReadState>
{
    public void Configure(EntityTypeBuilder<ReadState> builder)
    {
        builder.ToTable("ReadStates", t =>
        {
            t.HasCheckConstraint(
                "CK_ReadStates_Source",
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            t.HasCheckConstraint(
                "CK_ReadStates_Unread_NonNegative",
                "UnreadCount >= 0");

            t.HasCheckConstraint(
                "CK_ReadStates_Mention_NonNegative",
                "MentionCount >= 0");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        builder.Property(r => r.LastReadSequenceNumber)
               .HasDefaultValue(0);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(r => new { r.UserId, r.ChannelId })
               .IsUnique()
               .HasDatabaseName("UX_ReadStates_User_Channel")
               .HasFilter("[ChannelId] IS NOT NULL");

        builder.HasIndex(r => new { r.UserId, r.ConversationId })
               .IsUnique()
               .HasDatabaseName("UX_ReadStates_User_Conversation")
               .HasFilter("[ConversationId] IS NOT NULL");

        builder.HasIndex(r => r.UserId)
               .HasDatabaseName("IX_ReadStates_User");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Channel)
               .WithMany()
               .HasForeignKey(r => r.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Conversation)
               .WithMany()
               .HasForeignKey(r => r.ConversationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.LastReadMessage)
               .WithMany()
               .HasForeignKey(r => r.LastReadMessageId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}