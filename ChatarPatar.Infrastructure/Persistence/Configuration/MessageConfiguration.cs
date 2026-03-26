using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
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
                "CK_Messages_Source",
                "(ChannelId IS NOT NULL AND ConversationId IS NULL) OR " +
                "(ChannelId IS NULL AND ConversationId IS NOT NULL)");

            t.HasCheckConstraint(
                "CK_Messages_DmStatus",
                "DmStatus IS NULL OR DmStatus IN ('Sending','Sent','Delivered','Seen')");

            t.HasCheckConstraint(
                "CK_Messages_ThreadReplyRule",
                "(ThreadRootMessageId IS NULL) OR (ReplyCount = 0)");

            t.HasCheckConstraint(
                "CK_Messages_MessageType",
                "MessageType BETWEEN 1 AND 4");
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
        // Enum Conversion
        // ----------------------------

        builder.Property(m => m.DmStatus)
               .HasConversion(
                   v => v!.ToString(),
                   v => Enum.Parse<DmMessageStatusEnum>(v))
               .HasMaxLength(ValidationConstants.Message.Lengths.DmStatus)
               .IsRequired(false)
               .IsUnicode(true);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Channel)
               .WithMany()
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName("FK_Messages_Channel")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Conversation)
               .WithMany()
               .HasForeignKey(m => m.ConversationId)
               .HasConstraintName("FK_Messages_Conversation")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Sender)
               .WithMany()
               .HasForeignKey(m => m.SenderId)
               .HasConstraintName("FK_Messages_Sender")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Thread)
               .WithMany()
               .HasForeignKey(m => m.ThreadRootMessageId)
               .HasConstraintName("FK_Messages_Thread")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName("FK_Messages_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => new { m.ChannelId, m.SenderId, m.ClientMessageId })
                .IsUnique()
                .HasDatabaseName("UX_Messages_Channel_ClientMessage")
                .HasFilter("[ChannelId] IS NOT NULL");

        builder.HasIndex(m => new { m.ConversationId, m.SenderId, m.ClientMessageId })
                .IsUnique()
                .HasDatabaseName("UX_Messages_Conversation_ClientMessage")
                .HasFilter("[ConversationId] IS NOT NULL");

        builder.HasIndex(m => new { m.ThreadRootMessageId, m.CreatedAt })
               .HasDatabaseName("IX_Messages_ThreadRootMessageId")
               .HasFilter("[IsDeleted] = 0 AND [ThreadRootMessageId] IS NOT NULL");

        builder.HasIndex(m => new { m.ChannelId, m.SequenceNumber })
               .HasDatabaseName("IX_Messages_Channel_Active")
               .HasFilter("[IsDeleted] = 0 AND [ChannelId] IS NOT NULL");

        builder.HasIndex(m => new { m.ConversationId, m.SequenceNumber })
               .HasDatabaseName("IX_Messages_Conversation_Active")
               .HasFilter("[IsDeleted] = 0 AND [ConversationId] IS NOT NULL");

        builder.HasIndex(m => new { m.SenderId, m.CreatedAt})
               .HasDatabaseName("IX_Messages_SenderId_CreatedAt")
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}