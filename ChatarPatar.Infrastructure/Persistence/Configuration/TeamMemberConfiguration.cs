using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers", t =>
        {
            t.HasCheckConstraint(
                "CK_TeamMembers_Role",
                "Role IN ('TeamAdmin','TeamMember','TeamGuest')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.Role)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<TeamRoleEnum>(v))
               .HasMaxLength(FieldLengths.TeamFields.Role)
               .HasDefaultValue(TeamRoleEnum.TeamMember)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(m => m.JoinedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(m => m.IsMuted)
               .HasDefaultValue(false);

        builder.Property(m => m.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(m => m.TeamId)
               .HasDatabaseName("IX_TeamMembers_TeamId");

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName("IX_TeamMembers_UserId");

        builder.HasIndex(m => new { m.TeamId, m.UserId })
               .IsUnique()
               .HasDatabaseName("UX_TeamMembers_Active")
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Team)
               .WithMany(t => t.TeamMembers)
               .HasForeignKey(m => m.TeamId)
               .HasConstraintName("FK_TeamMembers_Team")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .HasConstraintName("FK_TeamMembers_User")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InvitedByUser)
               .WithMany()
               .HasForeignKey(m => m.InvitedByUserId)
               .HasConstraintName("FK_TeamMembers_Inviter")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedBy)
               .HasConstraintName("FK_TeamMembers_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UpdatedByUser)
               .WithMany()
               .HasForeignKey(m => m.UpdatedBy)
               .HasConstraintName("FK_TeamMembers_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName("FK_TeamMembers_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}