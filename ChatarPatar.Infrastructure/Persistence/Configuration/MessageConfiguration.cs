using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages", t =>
        {
            t.HasCheckConstraint(
                DbConstraints.Messages.CKMessageSource,
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            t.HasCheckConstraint(
                DbConstraints.Messages.CKSeenAfterDelivered,
                "DmSeenAt IS NULL OR DmDeliveredAt IS NULL OR DmSeenAt >= DmDeliveredAt");

            t.HasCheckConstraint(
                DbConstraints.Messages.CKThreadReplyRule,
                "(ThreadRootMessageId IS NULL) OR (ReplyCount = 0)");

            t.HasCheckConstraint(
                DbConstraints.Messages.CKType,
                "MessageType BETWEEN 1 AND 4");

            t.HasCheckConstraint(
                DbConstraints.Messages.CKNoSelfThread,
                "(ThreadRootMessageId IS NULL OR ThreadRootMessageId <> Id)"
            );

            t.HasCheckConstraint(
                DbConstraints.Messages.CKReplyState,
                "(ReplyCount = 0 AND LastReplyAt IS NULL OR ReplyCount > 0 AND LastReplyAt IS NOT NULL)"
            );

            t.HasCheckConstraint(
                DbConstraints.Messages.CKEditedState,
                "(IsEdited = 0 AND EditedAt IS NULL OR IsEdited = 1 AND EditedAt IS NOT NULL)"
            );
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.SequenceNumber)
               .UseIdentityColumn();

        builder.Property(m => m.Content)
               .HasMaxLength(ValidationConstants.Message.Lengths.Content)
               .IsUnicode(true);

        builder.Property(n => n.MessageType)
               .HasConversion<byte>();

        builder.Property(m => m.IsEdited)
               .HasDefaultValue(false);

        builder.Property(m => m.ReplyCount)
               .HasDefaultValue(0);

        builder.Property(m => m.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(m => m.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Channel)
               .WithMany()
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName(DbConstraints.Messages.FKChannel)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Conversation)
               .WithMany(m => m.Messages)
               .HasForeignKey(m => m.ConversationId)
               .HasConstraintName(DbConstraints.Messages.FKConversation)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Sender)
               .WithMany()
               .HasForeignKey(m => m.SenderId)
               .HasConstraintName(DbConstraints.Messages.FKSender)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Thread)
               .WithMany()
               .HasForeignKey(m => m.ThreadRootMessageId)
               .HasConstraintName(DbConstraints.Messages.FKThreadMessage)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName(DbConstraints.Messages.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => new { m.ChannelId, m.SenderId, m.ClientMessageId })
                .IsUnique()
                .HasDatabaseName(DbConstraints.Messages.UniqueChannelClientMessage)
                .HasFilter("[ChannelId] IS NOT NULL");

        builder.HasIndex(m => new { m.ConversationId, m.SenderId, m.ClientMessageId })
                .IsUnique()
                .HasDatabaseName(DbConstraints.Messages.UniqueConversationClientMessage)
                .HasFilter("[ConversationId] IS NOT NULL");

        builder.HasIndex(m => new { m.ThreadRootMessageId, m.CreatedAt })
               .HasDatabaseName(DbConstraints.Messages.IXThreadRootMessageId)
               .HasFilter("[IsDeleted] = 0 AND [ThreadRootMessageId] IS NOT NULL");

        builder.HasIndex(m => new { m.ChannelId, m.SequenceNumber })
               .HasDatabaseName(DbConstraints.Messages.IXActiveChannelMessage)
               .HasFilter("[IsDeleted] = 0 AND [ChannelId] IS NOT NULL");

        builder.HasIndex(m => new { m.ConversationId, m.SequenceNumber })
               .HasDatabaseName(DbConstraints.Messages.IXActiveConversationMessage)
               .HasFilter("[IsDeleted] = 0 AND [ConversationId] IS NOT NULL");

        builder.HasIndex(m => new { m.SenderId, m.CreatedAt })
               .HasDatabaseName(DbConstraints.Messages.IXSenderId)
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}