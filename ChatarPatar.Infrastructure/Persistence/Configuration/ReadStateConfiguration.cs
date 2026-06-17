using ChatarPatar.Common.Consts;
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
                DbConstraints.ReadStates.CKMessageSource,
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            t.HasCheckConstraint(
                DbConstraints.ReadStates.CKNonNegativeUnreadCount,
                "UnreadCount >= 0");

            t.HasCheckConstraint(
                DbConstraints.ReadStates.CKNonNegativeMentionCount,
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
               .HasDatabaseName(DbConstraints.ReadStates.UniqueReadStatePerChannel)
               .HasFilter("[ChannelId] IS NOT NULL");

        builder.HasIndex(r => new { r.UserId, r.ConversationId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.ReadStates.UniqueReadStatePerConversation)
               .HasFilter("[ConversationId] IS NOT NULL");

        builder.HasIndex(r => r.UserId)
               .HasDatabaseName(DbConstraints.ReadStates.IXUserId);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Channel)
               .WithMany(r => r.ReadStates)
               .HasForeignKey(r => r.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Conversation)
               .WithMany(r => r.ReadStates)
               .HasForeignKey(r => r.ConversationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.LastReadMessage)
               .WithMany()
               .HasForeignKey(r => r.LastReadMessageId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}