using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class FilesConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("Files", t =>
        {
            // ----------------------------
            // Check Constraint
            // ----------------------------

            t.HasCheckConstraint(
                DbConstraints.Files.CKType,
                "FileType IN ('image','video','audio','document','code','archive','other')");

            t.HasCheckConstraint(
                DbConstraints.Files.CKUsageContext,
                "UsageContext IN ('avatar','attachment','org_logo','team_icon', 'conversation_logo')");

            t.HasCheckConstraint(
                DbConstraints.Files.CKStatus,
                "Status IN ('pending','attached')");

            // pending → no scope, ExpiresAt required
            // attached → exactly one scope, ExpiresAt NULL
            t.HasCheckConstraint(
                DbConstraints.Files.CKScopeRule,
                @"(
                    UsageContext = 'attachment'
                    AND Status   = 'pending'
                    AND UserId         IS NULL
                    AND OrgId          IS NULL
                    AND TeamId         IS NULL
                    AND ChannelId      IS NULL
                    AND ConversationId IS NULL
                  )
                  OR
                  (
                    Status = 'attached'
                    AND (
                        (CASE WHEN UserId         IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN OrgId          IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN TeamId         IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN ChannelId      IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN ConversationId IS NOT NULL THEN 1 ELSE 0 END)
                    ) = 1
                  )");

            // pending → ExpiresAt must be set; attached → must be NULL
            t.HasCheckConstraint(
                DbConstraints.Files.CKExpiresAtRule,
                @"(Status = 'pending'  AND ExpiresAt IS NOT NULL)
                  OR
                  (Status = 'attached' AND ExpiresAt IS NULL)");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required string fields
        builder.Property(f => f.PublicId)
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.PublicId)
               .IsUnicode(true);

        builder.Property(f => f.Url)
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.Url)
               .IsUnicode(true);

        builder.Property(f => f.ThumbnailUrl)
               .HasMaxLength(ValidationConstants.File.Lengths.ThumbnailUrl)
               .IsUnicode(true);

        builder.Property(f => f.MimeType)
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.MimeType)
               .IsUnicode(true);

        builder.Property(f => f.OriginalName)
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.OriginalName)
               .IsUnicode(true);

        // Enum stored as lowercase string
        builder.Property(f => f.FileType)
               .HasConversion(
                   v => v.ToString().ToLower(),
                   v => Enum.Parse<FileTypeEnum>(v, true))
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.FileType)
               .IsUnicode(true);

        builder.Property(m => m.UsageContext)
               .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<FileUsageContextEnum>(v, true))
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.UsageContext)
               .IsUnicode(true);

        builder.Property(f => f.Status)
               .HasConversion(
                   v => v.ToString().ToLower(),
                   v => Enum.Parse<FileStatusEnum>(v, true))
               .IsRequired()
               .HasMaxLength(ValidationConstants.File.Lengths.Status)
               .HasDefaultValue(FileStatusEnum.Attached)
               .IsUnicode(true);

        builder.Property(f => f.SizeInBytes)
               .IsRequired();

        builder.Property(f => f.ExpiresAt)
               .IsRequired(false);

        builder.Property(f => f.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(f => f.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(f => f.UploadedByUserId)
               .HasDatabaseName(DbConstraints.Files.IXUploadedByUserId);

        builder.HasIndex(f => f.UsageContext)
               .HasDatabaseName(DbConstraints.Files.IXUsageContext);

        // Filtered index: cleanup job hits this for expired pending files
        builder.HasIndex(f => f.ExpiresAt)
               .HasFilter($"Status = 'pending' AND IsDeleted = 0")
               .HasDatabaseName(DbConstraints.Files.IXPendingExpiry);

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
               .HasConstraintName(DbConstraints.Files.FKUploadedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.CreatedByUser)
               .WithMany()
               .HasForeignKey(f => f.CreatedBy)
               .HasConstraintName(DbConstraints.Files.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.UpdatedByUser)
               .WithMany()
               .HasForeignKey(f => f.UpdatedBy)
               .HasConstraintName(DbConstraints.Files.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.DeletedByUser)
               .WithMany()
               .HasForeignKey(f => f.DeletedBy)
               .HasConstraintName(DbConstraints.Files.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft delete filter
        // ----------------------------

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}