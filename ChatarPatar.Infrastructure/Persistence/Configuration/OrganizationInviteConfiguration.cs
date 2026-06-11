using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class OrganizationInviteConfiguration : IEntityTypeConfiguration<OrganizationInvite>
{
    public void Configure(EntityTypeBuilder<OrganizationInvite> builder)
    {
        builder.ToTable("OrganizationInvites", t =>
        {
            t.HasCheckConstraint(
                DbConstraints.OrganizationInvites.CKRole,
                "Role IN ('OrgOwner','OrgAdmin','OrgMember','OrgGuest')");

            t.HasCheckConstraint(
                DbConstraints.OrganizationInvites.CKUsedConsistency,
                @"(IsUsed = 0 AND UsedAt IS NULL AND UsedBy IS NULL) OR
                  (IsUsed = 1 AND UsedAt IS NOT NULL AND UsedBy IS NOT NULL)");

            t.HasCheckConstraint(
                DbConstraints.OrganizationInvites.CKFailedAttempts,
                "[FailedAttempts] >= 0");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(o => o.Email)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Email)
               .IsUnicode(true);

        builder.Property(o => o.Token)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Token)
               .IsUnicode(true);

        builder.Property(o => o.Role)
               .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<OrganizationRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Organization.Lengths.Role)
               .HasDefaultValue(OrganizationRoleEnum.OrgMember)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(o => o.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(o => o.IsUsed)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(o => o.FailedAttempts)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(o => o.VoidReason)
               .HasMaxLength(ValidationConstants.Organization.Lengths.VoidReason)
               .IsUnicode(true);

        builder.Property(o => o.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");
        
        builder.Property(o => o.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique constraint (Token)
        // ----------------------------

        builder.HasIndex(o => o.Token)
               .IsUnique()
               .HasDatabaseName(DbConstraints.OrganizationInvites.UniqueToken);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(o => o.OrganizationId)
               .HasDatabaseName(DbConstraints.OrganizationInvites.IXOrgId)
               .HasFilter("[IsUsed] = 0");

        builder.HasIndex(o => o.Email)
               .HasDatabaseName(DbConstraints.OrganizationInvites.IXEmail)
               .HasFilter("[IsUsed] = 0");

        builder.HasIndex(o => o.ExpiresAt)
               .HasDatabaseName(DbConstraints.OrganizationInvites.IXExpiresAt)
               .HasFilter("[IsUsed] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(o => o.Organization)
               .WithMany()
               .HasForeignKey(o => o.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName(DbConstraints.OrganizationInvites.FKOrg);

        builder.HasOne(o => o.CreatedByUser)
               .WithMany()
               .HasForeignKey(o => o.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName(DbConstraints.OrganizationInvites.FKCreatedByUser);

        builder.HasOne(o => o.UsedByUser)
               .WithMany()
               .HasForeignKey(o => o.UsedBy)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName(DbConstraints.OrganizationInvites.FKUsedByUser);
    }
}
