using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams", t =>
        {
            t.HasCheckConstraint(
                "CK_Teams_ArchiveState",
                "(IsArchived = 0 AND ArchivedAt IS NULL AND ArchivedBy IS NULL) OR (IsArchived = 1 AND ArchivedAt IS NOT NULL)"
            );
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Team.Lengths.Name)
               .IsUnicode(true);

        builder.Property(t => t.Description)
               .HasMaxLength(ValidationConstants.Team.Lengths.Description)
               .IsUnicode(true);

        builder.Property(t => t.IsPrivate)
               .HasDefaultValue(false);

        builder.Property(t => t.IsArchived)
               .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(t => t.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(t => t.OrgId)
               .HasDatabaseName("IX_Teams_OrgId");

        builder.HasIndex(t => new { t.OrgId, t.IsArchived })
               .HasDatabaseName("IX_Teams_Archived");

        builder.HasIndex(m => new { m.OrgId, m.Name })
                .IsUnique()
                .HasDatabaseName("UX_Teams_Name")
                .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(t => t.Organization)
               .WithMany(t => t.Teams)
               .HasForeignKey(t => t.OrgId)
               .HasConstraintName("FK_Teams_Org")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.IconFile)
               .WithMany()
               .HasForeignKey(t => t.IconFileId)
               .HasConstraintName("FK_Teams_Icon")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ArchivedByUser)
               .WithMany()
               .HasForeignKey(t => t.ArchivedBy)
               .HasConstraintName("FK_Teams_Archiver")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
               .WithMany()
               .HasForeignKey(t => t.CreatedBy)
               .HasConstraintName("FK_Teams_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.UpdatedByUser)
               .WithMany()
               .HasForeignKey(t => t.UpdatedBy)
               .HasConstraintName("FK_Teams_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DeletedByUser)
               .WithMany()
               .HasForeignKey(t => t.DeletedBy)
               .HasConstraintName("FK_Teams_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}