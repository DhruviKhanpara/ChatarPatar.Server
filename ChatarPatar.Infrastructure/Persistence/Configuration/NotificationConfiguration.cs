using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint(
                "CK_Notifications_ReadConsistency",
                "(IsRead = 0 AND ReadAt IS NULL) OR " +
                "(IsRead = 1 AND ReadAt IS NOT NULL)");

            t.HasCheckConstraint(
                "CK_Notifications_Type",
                "Type BETWEEN 1 AND 8");
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(n => n.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(n => n.Type)
               .HasConversion<byte>();

        builder.Property(n => n.Preview)
            .HasMaxLength(FieldLengths.NotificationFields.Preview)
            .IsUnicode(true);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt })
               .HasDatabaseName("IX_Notifications_UserId");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(n => n.Recipient)
               .WithMany()
               .HasForeignKey(n => n.RecipientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Actor)
               .WithMany()
               .HasForeignKey(n => n.ActorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Message)
               .WithMany()
               .HasForeignKey(n => n.MessageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Channel)
               .WithMany()
               .HasForeignKey(n => n.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Conversation)
               .WithMany()
               .HasForeignKey(n => n.ConversationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Team)
               .WithMany()
               .HasForeignKey(n => n.TeamId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}