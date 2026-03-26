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
                "CK_PinnedMessages_Source",
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            // Unpin consistency
            t.HasCheckConstraint(
                "CK_PinnedMessages_UnpinConsistency",
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
               .HasConstraintName("FK_PinnedMessages_Message")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Channel)
               .WithMany()
               .HasForeignKey(p => p.ChannelId)
               .HasConstraintName("FK_PinnedMessages_Channel")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Conversation)
               .WithMany()
               .HasForeignKey(p => p.ConversationId)
               .HasConstraintName("FK_PinnedMessages_Conv")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PinnedByUser)
               .WithMany()
               .HasForeignKey(p => p.PinnedByUserId)
               .HasConstraintName("FK_PinnedMessages_PinnedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UnPinnedByUser)
               .WithMany()
               .HasForeignKey(p => p.UnPinnedByUserId)
               .HasConstraintName("FK_PinnedMessages_UnPinnedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(p => new { p.MessageId, p.ConversationId })
               .IsUnique()
               .HasDatabaseName("UQ_PinnedConversationMessages");

        builder.HasIndex(p => new { p.MessageId, p.ChannelId })
               .IsUnique()
               .HasDatabaseName("UX_Pinned_Channel_Active")
               .HasFilter("[ChannelId] IS NOT NULL AND [UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.MessageId, p.ConversationId })
               .IsUnique()
               .HasDatabaseName("UX_Pinned_Conversation_Active")
               .HasFilter("[ConversationId] IS NOT NULL AND [UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.ChannelId, p.PinnedAt })
               .HasDatabaseName("IX_Pinned_Channel_Active")
               .HasFilter("[UnPinnedAt] IS NULL");

        builder.HasIndex(p => new { p.ConversationId, p.PinnedAt })
               .HasDatabaseName("IX_Pinned_Conversation_Active")
               .HasFilter("[UnPinnedAt] IS NULL");
    }
}