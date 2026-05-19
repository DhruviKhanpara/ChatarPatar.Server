using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(o => o.Name)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Name)
               .IsUnicode(true);

        builder.Property(o => o.Slug)
                .HasConversion(
                    v => v.ToLower(),
                    v => v)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Slug)
               .IsUnicode(true);

        builder.Property(o => o.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(o => o.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // Unique Constraints
        builder.HasIndex(o => o.Slug)
               .IsUnique()
               .HasDatabaseName(DbConstraints.Organizations.UniqueSlug);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(o => o.LogoFile)
               .WithMany()
               .HasForeignKey(o => o.LogoFileId)
               .HasConstraintName(DbConstraints.Organizations.FKLogoFile)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.CreatedByUser)
               .WithMany()
               .HasForeignKey(o => o.CreatedBy)
               .HasConstraintName(DbConstraints.Organizations.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.UpdatedByUser)
               .WithMany()
               .HasForeignKey(o => o.UpdatedBy)
               .HasConstraintName(DbConstraints.Organizations.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.DeletedByUser)
               .WithMany()
               .HasForeignKey(o => o.DeletedBy)
               .HasConstraintName(DbConstraints.Organizations.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft delete filter
        // ----------------------------

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}