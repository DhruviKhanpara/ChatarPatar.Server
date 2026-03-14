using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        builder.ToTable("MessageMentions", t =>
        {
            t.HasCheckConstraint(
                "CK_MessageMentions_Source",
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique Constraints
        // ----------------------------

        builder.HasIndex(m => new { m.MessageId, m.MentionedUserId })
               .IsUnique()
               .HasDatabaseName("UQ_MessageMentions");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Message)
               .WithMany()
               .HasForeignKey(m => m.MessageId)
               .HasConstraintName("FK_MessageMentions_Message")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.MentionedUser)
               .WithMany()
               .HasForeignKey(m => m.MentionedUserId)
               .HasConstraintName("FK_MessageMentions_User")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Channel)
               .WithMany()
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName("FK_MessageMentions_Channel")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Conversation)
               .WithMany()
               .HasForeignKey(m => m.ConversationId)
               .HasConstraintName("FK_MessageMentions_Conv")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => new { m.MentionedUserId, m.ChannelId, m.CreatedAt })
               .HasDatabaseName("IX_MessageMentions_UserChannel");

        builder.HasIndex(m => new { m.MentionedUserId, m.ConversationId, m.CreatedAt })
               .HasDatabaseName("IX_MessageMentions_UserConv");
    }
}