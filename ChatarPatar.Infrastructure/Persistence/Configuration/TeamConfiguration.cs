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
                DbConstraints.Teams.CKArchiveState,
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
               .HasDatabaseName(DbConstraints.Teams.IXOrgId);

        builder.HasIndex(t => new { t.OrgId, t.IsArchived })
               .HasDatabaseName(DbConstraints.Teams.IXTeamArchivedInOrg);

        builder.HasIndex(m => new { m.OrgId, m.Name })
                .IsUnique()
                .HasDatabaseName(DbConstraints.Teams.UniqueNamePerOrg)
                .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(t => t.Organization)
               .WithMany(t => t.Teams)
               .HasForeignKey(t => t.OrgId)
               .HasConstraintName(DbConstraints.Teams.FKOrg)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.IconFile)
               .WithMany()
               .HasForeignKey(t => t.IconFileId)
               .HasConstraintName(DbConstraints.Teams.FKIconFile)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ArchivedByUser)
               .WithMany()
               .HasForeignKey(t => t.ArchivedBy)
               .HasConstraintName(DbConstraints.Teams.FKArchiver)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
               .WithMany()
               .HasForeignKey(t => t.CreatedBy)
               .HasConstraintName(DbConstraints.Teams.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.UpdatedByUser)
               .WithMany()
               .HasForeignKey(t => t.UpdatedBy)
               .HasConstraintName(DbConstraints.Teams.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DeletedByUser)
               .WithMany()
               .HasForeignKey(t => t.DeletedBy)
               .HasConstraintName(DbConstraints.Teams.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}