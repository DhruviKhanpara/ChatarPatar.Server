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

        builder.HasIndex(o => o.Slug)
               .IsUnique()
               .HasDatabaseName("UQ_Organizations_Slug");

        builder.Property(o => o.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(o => o.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(o => o.LogoFile)
               .WithMany()
               .HasForeignKey(o => o.LogoFileId)
               .HasConstraintName("FK_Organizations_Logo")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.CreatedByUser)
               .WithMany()
               .HasForeignKey(o => o.CreatedBy)
               .HasConstraintName("FK_Organizations_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.UpdatedByUser)
               .WithMany()
               .HasForeignKey(o => o.UpdatedBy)
               .HasConstraintName("FK_Organizations_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.DeletedByUser)
               .WithMany()
               .HasForeignKey(o => o.DeletedBy)
               .HasConstraintName("FK_Organizations_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft delete filter
        // ----------------------------

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}