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
                "CK_OrgInvites_Role",
                "Role IN ('OrgOwner','OrgAdmin','OrgMember','OrgGuest')");

            t.HasCheckConstraint(
                "CK_OrgInvites_UsedConsistency",
                @"(IsUsed = 0 AND UsedAt IS NULL AND UsedBy IS NULL) OR
                  (IsUsed = 1 AND UsedAt IS NOT NULL AND UsedBy IS NOT NULL)");

            t.HasCheckConstraint(
                "CK_OrgInvites_FailedAttempts",
                "[FailedAttempts] >= 0");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(o => o.Email)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Email)
               .IsUnicode(true);

        builder.Property(o => o.Token)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Organization.Lengths.Token)
               .IsUnicode(true);

        builder.Property(m => m.Role)
               .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<OrganizationRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Organization.Lengths.Role)
               .HasDefaultValue(OrganizationRoleEnum.OrgMember)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(m => m.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(m => m.IsUsed)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(m => m.FailedAttempts)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");
        
        builder.Property(m => m.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique constraint (Token)
        // ----------------------------

        builder.HasIndex(x => x.Token)
               .IsUnique()
               .HasDatabaseName("UQ_OrgInvites_Token");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(x => x.OrganizationId)
               .HasDatabaseName("IX_OrgInvites_OrgId")
               .HasFilter("[IsUsed] = 0");

        builder.HasIndex(x => x.Email)
               .HasDatabaseName("IX_OrgInvites_Email")
               .HasFilter("[IsUsed] = 0");

        builder.HasIndex(x => x.ExpiresAt)
               .HasDatabaseName("IX_OrgInvites_ExpiresAt")
               .HasFilter("[IsUsed] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(x => x.Organization)
               .WithMany()
               .HasForeignKey(x => x.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_OrgInvites_Org");

        builder.HasOne(x => x.CreatedByUser)
               .WithMany()
               .HasForeignKey(x => x.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_OrgInvites_CreatedBy");

        builder.HasOne(x => x.UsedByUser)
               .WithMany()
               .HasForeignKey(x => x.UsedBy)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_OrgInvites_UsedBy");
    }
}
