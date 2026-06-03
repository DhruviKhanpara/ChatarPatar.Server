using ChatarPatar.Common.Consts;
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
                DbConstraints.MessageMentions.CKMessageSource,
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
               .HasDatabaseName(DbConstraints.MessageMentions.UniqueMentionUserPerMessage);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Message)
               .WithMany(m => m.MessageMentions)
               .HasForeignKey(m => m.MessageId)
               .HasConstraintName(DbConstraints.MessageMentions.FKMessage)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.MentionedUser)
               .WithMany()
               .HasForeignKey(m => m.MentionedUserId)
               .HasConstraintName(DbConstraints.MessageMentions.FKMentionedUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Channel)
               .WithMany()
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName(DbConstraints.MessageMentions.FKChannel)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Conversation)
               .WithMany()
               .HasForeignKey(m => m.ConversationId)
               .HasConstraintName(DbConstraints.MessageMentions.FKConversation)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => new { m.MentionedUserId, m.ChannelId, m.CreatedAt })
               .HasDatabaseName(DbConstraints.MessageMentions.IXMentionUserInChannel);

        builder.HasIndex(m => new { m.MentionedUserId, m.ConversationId, m.CreatedAt })
               .HasDatabaseName(DbConstraints.MessageMentions.IXMentionUserInConversation);
    }
}