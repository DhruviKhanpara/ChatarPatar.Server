using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class MessageReceiptConfiguration : IEntityTypeConfiguration<MessageReceipt>
{
    public void Configure(EntityTypeBuilder<MessageReceipt> builder)
    {
        builder.ToTable("MessageReceipts", t =>
        {
            t.HasCheckConstraint(
                "CK_MessageReceipts_SeenAfterDelivered",
                "SeenAt IS NULL OR DeliveredAt IS NULL OR SeenAt >= DeliveredAt");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(r => r.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(r => new { r.MessageId, r.UserId })
               .IsUnique()
               .HasDatabaseName("UQ_MessageReceipts");

        builder.HasIndex(r => new { r.UserId, r.MessageId })
               .HasDatabaseName("IX_MessageReceipts_Message");

        builder.HasIndex(r => new { r.UserId, r.SeenAt })
               .HasDatabaseName("IX_MessageReceipts_User_Seen");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.Message)
               .WithMany()
               .HasForeignKey(r => r.MessageId)
               .HasConstraintName("FK_MessageReceipts_Message")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .HasConstraintName("FK_MessageReceipts_User")
               .OnDelete(DeleteBehavior.Restrict);
    }
}