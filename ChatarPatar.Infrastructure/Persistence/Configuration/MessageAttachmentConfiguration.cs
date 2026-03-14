using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.DisplayOrder)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(a => a.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique Constraints
        // ----------------------------

        builder.HasIndex(a => new { a.MessageId, a.FileId })
               .IsUnique()
               .HasDatabaseName("UQ_MessageAttachments_File");

        builder.HasIndex(a => new { a.MessageId, a.DisplayOrder })
               .IsUnique()
               .HasDatabaseName("UQ_MessageAttachments_Order");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(a => a.MessageId)
               .HasDatabaseName("IX_MessageAttachments_MessageId");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(a => a.Message)
               .WithMany()
               .HasForeignKey(a => a.MessageId)
               .HasConstraintName("FK_MessageAttachments_Message")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.File)
               .WithMany()
               .HasForeignKey(a => a.FileId)
               .HasConstraintName("FK_MessageAttachments_File")
               .OnDelete(DeleteBehavior.Restrict);
    }
}