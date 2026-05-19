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
                DbConstraints.TeamMembers.CKRole,
                "Role IN ('TeamAdmin','TeamMember','TeamGuest')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.Role)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<TeamRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Team.Lengths.Role)
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
               .HasDatabaseName(DbConstraints.TeamMembers.IXTeamId);

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName(DbConstraints.TeamMembers.IXUserId);

        builder.HasIndex(m => new { m.TeamId, m.UserId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.TeamMembers.UniqueActiveTeamMembers)
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Team)
               .WithMany(t => t.TeamMembers)
               .HasForeignKey(m => m.TeamId)
               .HasConstraintName(DbConstraints.TeamMembers.FKTeam)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .HasConstraintName(DbConstraints.TeamMembers.FKUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InvitedByUser)
               .WithMany()
               .HasForeignKey(m => m.InvitedByUserId)
               .HasConstraintName(DbConstraints.TeamMembers.FKInviter)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedBy)
               .HasConstraintName(DbConstraints.TeamMembers.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UpdatedByUser)
               .WithMany()
               .HasForeignKey(m => m.UpdatedBy)
               .HasConstraintName(DbConstraints.TeamMembers.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName(DbConstraints.TeamMembers.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}