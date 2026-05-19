using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("OtpVerifications", t =>
        {
            t.HasCheckConstraint(
                DbConstraints.OtpVerifications.CKPurpose,
                "[Purpose] = 'PasswordReset' OR [Purpose] = 'EmailVerification'");

            t.HasCheckConstraint(
                DbConstraints.OtpVerifications.CKUsedConsistency,
                "(IsUsed = 0 AND UsedAt IS NULL) OR (IsUsed = 1 AND UsedAt IS NOT NULL)");

            t.HasCheckConstraint(
               DbConstraints.OtpVerifications.CKFailedAttempts,
               "[FailedAttempts] >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.OtpHash)
               .IsRequired()
               .HasMaxLength(ValidationConstants.OtpVerification.Lengths.OtpHash)
               .IsUnicode(false);

        builder.Property(x => x.Purpose)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<OtpPurposeEnum>(v))
               .IsRequired()
               .HasMaxLength(ValidationConstants.OtpVerification.Lengths.Purpose)
               .IsUnicode(false);

        builder.Property(x => x.IPAddress)
               .HasMaxLength(ValidationConstants.OtpVerification.Lengths.IPAddress)
               .IsUnicode(false);

        builder.Property(x => x.IsUsed)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(x => x.FailedAttempts)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        // Fast lookup: find active OTP for a user + purpose
        builder.HasIndex(x => new { x.UserId, x.Purpose })
               .HasDatabaseName(DbConstraints.OtpVerifications.IXUserPurposeUnused)
               .HasFilter("[IsUsed] = 0");

        // Cooldown check: find the latest OTP per user + purpose (all rows, not filtered)
        builder.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAt })
               .HasDatabaseName(DbConstraints.OtpVerifications.IXUserPurposeCreatedAt);

        // Cleanup job: find all expired rows
        builder.HasIndex(x => x.ExpiresAt)
               .HasDatabaseName(DbConstraints.OtpVerifications.IXUnusedExpiredAt)
               .HasFilter("[IsUsed] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName(DbConstraints.OtpVerifications.FKUser);
    }
}
