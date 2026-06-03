using ChatarPatar.Common.Consts;
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
               .HasDatabaseName(DbConstraints.MessageAttachments.UniqueFilePerMessage);

        builder.HasIndex(a => new { a.MessageId, a.DisplayOrder })
               .IsUnique()
               .HasDatabaseName(DbConstraints.MessageAttachments.UniqueDisplayOrderInMessage);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(a => a.MessageId)
               .HasDatabaseName(DbConstraints.MessageAttachments.IXMessageId);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(a => a.Message)
               .WithMany(m => m.MessageAttachments)
               .HasForeignKey(a => a.MessageId)
               .HasConstraintName(DbConstraints.MessageAttachments.FKMessage)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.File)
               .WithMany()
               .HasForeignKey(a => a.FileId)
               .HasConstraintName(DbConstraints.MessageAttachments.FKFile)
               .OnDelete(DeleteBehavior.Restrict);
    }
}