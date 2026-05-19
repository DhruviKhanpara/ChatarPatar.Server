using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class PinnedMessageConfiguration : IEntityTypeConfiguration<PinnedMessage>
{
    public void Configure(EntityTypeBuilder<PinnedMessage> builder)
    {
        builder.ToTable("PinnedMessages", t =>
        {
            // XOR rule: exactly one scope
            t.HasCheckConstraint(
                DbConstraints.PinnedMessages.CKMessageSource,
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            // Unpin consistency
            t.HasCheckConstraint(
                DbConstraints.PinnedMessages.CKUnpinConsistency,
                "(UnPinnedAt IS NULL AND UnPinnedByUserId IS NULL) OR " +
                "(UnPinnedAt IS NOT NULL AND UnPinnedByUserId IS NOT NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.PinnedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(p => p.ContentSnapshot)
               .HasMaxLength(ValidationConstants.Message.Lengths.ContentSnapshot)
               .IsUnicode(true);

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(p => p.Message)
               .WithMany()
               .HasForeignKey(p => p.MessageId)
               .HasConstraintName(DbConstraints.PinnedMessages.FKMessage)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Channel)
               .WithMany()
               .HasForeignKey(p => p.ChannelId)
               .HasConstraintName(DbConstraints.PinnedMessages.FKChannel)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Conversation)
               .WithMany()
               .HasForeignKey(p => p.ConversationId)
               .HasConstraintName(DbConstraints.PinnedMessages.FKConversation)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PinnedByUser)
               .WithMany()
               .HasForeignKey(p => p.PinnedByUserId)
               .HasConstraintName(DbConstraints.PinnedMessages.FKPinnedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UnPinnedByUser)
               .WithMany()
               .HasForeignKey(p => p.UnPinnedByUserId)
               .HasConstraintName(DbConstraints.PinnedMessages.FKUnPinnedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(p => new { p.MessageId, p.ChannelId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.PinnedMessages.UniquePinnedMessagePerChannel)
               .HasFilter("[ChannelId] IS NOT NULL AND [UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.MessageId, p.ConversationId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.PinnedMessages.UniquePinnedMessagePerConversation)
               .HasFilter("[ConversationId] IS NOT NULL AND [UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.ChannelId, p.PinnedAt })
               .HasDatabaseName(DbConstraints.PinnedMessages.IXChannelMessagePinnedAt)
               .HasFilter("[UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.ConversationId, p.PinnedAt })
               .HasDatabaseName(DbConstraints.PinnedMessages.IXConversationMessagePinnedAt)
               .HasFilter("[UnPinnedAt] IS NULL");
    }
}