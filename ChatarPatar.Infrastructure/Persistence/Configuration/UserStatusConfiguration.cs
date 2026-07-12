using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class UserStatusConfiguration : IEntityTypeConfiguration<UserStatus>
{
    public void Configure(EntityTypeBuilder<UserStatus> builder)
    {
        builder.ToTable("UserStatus", t =>
        {
            t.HasCheckConstraint(
                DbConstraints.UserStatuses.CKStatus,
                "Status BETWEEN 0 AND 2");

            t.HasCheckConstraint(
                DbConstraints.UserStatuses.CKCustomStatus,
                "CustomStatus IS NULL OR CustomStatus BETWEEN 1 AND 6");
        });

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.Status)
               .HasConversion<byte>();

        builder.Property(u => u.CustomStatus)
               .HasConversion<byte?>();

        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.LastSeenAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(u => u.Status)
               .HasDatabaseName(DbConstraints.UserStatuses.IXStatus);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(u => u.User)
               .WithOne()
               .HasForeignKey<UserStatus>(u => u.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}