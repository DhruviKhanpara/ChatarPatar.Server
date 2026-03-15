using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class FilesConfiguration : IEntityTypeConfiguration<Files>
{
    public void Configure(EntityTypeBuilder<Files> builder)
    {
        builder.ToTable("Files", t =>
        {
            // ----------------------------
            // Check Constraint
            // ----------------------------

            t.HasCheckConstraint(
                "CK_Files_FileType",
                "FileType IN ('image','video','audio','document','code','archive','other')");

            t.HasCheckConstraint(
                "CK_Files_UsageContext",
                "UsageContext IN ('avatar','attachment','org_logo','team_icon', 'conversation_logo')");

            t.HasCheckConstraint(
                "CK_Files_OnlyOneScope",
                @"
                (
                    (CASE WHEN UserId IS NOT NULL THEN 1 ELSE 0 END) +
                    (CASE WHEN OrgId IS NOT NULL THEN 1 ELSE 0 END) +
                    (CASE WHEN TeamId IS NOT NULL THEN 1 ELSE 0 END) +
                    (CASE WHEN ChannelId IS NOT NULL THEN 1 ELSE 0 END) +
                    (CASE WHEN ConversationId IS NOT NULL THEN 1 ELSE 0 END)
                ) <= 1
                ");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required string fields
        builder.Property(f => f.PublicId)
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.PublicId)
               .IsUnicode(true);

        builder.Property(f => f.Url)
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.Url)
               .IsUnicode(true);

        builder.Property(f => f.ThumbnailUrl)
               .HasMaxLength(FieldLengths.FileFields.ThumbnailUrl)
               .IsUnicode(true);

        builder.Property(f => f.MimeType)
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.MimeType)
               .IsUnicode(true);

        builder.Property(f => f.OriginalName)
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.OriginalName)
               .IsUnicode(true);

        // Enum stored as lowercase string
        builder.Property(f => f.FileType)
               .HasConversion(
                   v => v.ToString().ToLower(),
                   v => Enum.Parse<FileTypeEnum>(v, true))
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.FileType)
               .IsUnicode(true);

        builder.Property(m => m.UsageContext)
               .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<FileUsageContextEnum>(v, true))
               .IsRequired()
               .HasMaxLength(FieldLengths.FileFields.UsageContext)
               .IsUnicode(true);

        builder.Property(f => f.SizeInBytes)
               .IsRequired();

        builder.Property(f => f.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(f => f.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(f => f.UploadedByUserId)
               .HasDatabaseName("IX_Files_UploadedBy");

        builder.HasIndex(f => f.UsageContext)
               .HasDatabaseName("IX_Files_UsageContext");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Organization)
                .WithMany()
                .HasForeignKey(f => f.OrgId)
                .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Team)
               .WithMany()
               .HasForeignKey(f => f.TeamId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Channel)
               .WithMany()
               .HasForeignKey(f => f.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Conversation)
               .WithMany()
               .HasForeignKey(f => f.ConversationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.UploadedByUser)
               .WithMany()
               .HasForeignKey(f => f.UploadedByUserId)
               .HasConstraintName("FK_Files_UploadedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.CreatedByUser)
               .WithMany()
               .HasForeignKey(f => f.CreatedBy)
               .HasConstraintName("FK_Files_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.UpdatedByUser)
               .WithMany()
               .HasForeignKey(f => f.UpdatedBy)
               .HasConstraintName("FK_Files_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.DeletedByUser)
               .WithMany()
               .HasForeignKey(f => f.DeletedBy)
               .HasConstraintName("FK_Files_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft delete filter
        // ----------------------------

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}