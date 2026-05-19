using ChatarPatar.Common.Consts;
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
                DbConstraints.MessageReceipts.CKSeenAfterDelivered,
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
               .HasDatabaseName(DbConstraints.MessageReceipts.UniqueReceiptPerMessage);

        builder.HasIndex(r => new { r.UserId, r.MessageId })
               .HasDatabaseName(DbConstraints.MessageReceipts.IXUserMessage);

        builder.HasIndex(r => new { r.UserId, r.SeenAt })
               .HasDatabaseName(DbConstraints.MessageReceipts.IXUserSeenAt);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.Message)
               .WithMany()
               .HasForeignKey(r => r.MessageId)
               .HasConstraintName(DbConstraints.MessageReceipts.FKMessage)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .HasConstraintName(DbConstraints.MessageReceipts.FKUser)
               .OnDelete(DeleteBehavior.Restrict);
    }
}