using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMembers", t =>
        {
            t.HasCheckConstraint(
                "CK_OrgMembers_Role",
                "Role IN ('OrgOwner','OrgAdmin','OrgMember','OrgGuest')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.Role)
               .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<OrganizationRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Organization.Lengths.Role)
               .HasDefaultValue(OrganizationRoleEnum.OrgMember)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(m => m.JoinedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(m => m.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => m.OrgId)
               .HasDatabaseName("IX_OrgMembers_OrgId");

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName("IX_OrgMembers_UserId");
        
        builder.HasIndex(m => new { m.OrgId, m.UserId })
                .IsUnique()
                .HasDatabaseName("UX_OrgMembers_Active")
                .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Organization)
               .WithMany(o => o.OrganizationMembers)
               .HasForeignKey(m => m.OrgId)
               .HasConstraintName("FK_OrgMembers_Org")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .HasConstraintName("FK_OrgMembers_User")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InvitedByUser)
               .WithMany()
               .HasForeignKey(m => m.InvitedByUserId)
               .HasConstraintName("FK_OrgMembers_InvitedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedBy)
               .HasConstraintName("FK_OrgMembers_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UpdatedByUser)
               .WithMany()
               .HasForeignKey(m => m.UpdatedBy)
               .HasConstraintName("FK_OrgMembers_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName("FK_OrgMembers_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}