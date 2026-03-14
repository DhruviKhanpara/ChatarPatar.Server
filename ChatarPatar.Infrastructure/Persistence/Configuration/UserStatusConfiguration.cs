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
                "CK_UserStatus_Status",
                "Status BETWEEN 0 AND 2");

            t.HasCheckConstraint(
                "CK_UserStatus_CustomStatus",
                "CustomStatus IS NULL OR CustomStatus BETWEEN 1 AND 6");

            t.HasCheckConstraint(
                "CK_UserStatus_Logical",
                "(Status = 0 AND CustomStatus IS NULL) OR (Status IN (1,2))");
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
               .HasDatabaseName("IX_UserStatus_Status");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(u => u.User)
               .WithOne()
               .HasForeignKey<UserStatus>(u => u.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}