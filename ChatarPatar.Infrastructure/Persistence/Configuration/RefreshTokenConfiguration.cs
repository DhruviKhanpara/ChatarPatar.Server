using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", t =>
        {
            t.HasCheckConstraint(
                DbConstraints.RefreshTokens.CKRevokeConsistency,
                "(IsRevoked = 0 AND RevokedAt IS NULL) OR " +
                "(IsRevoked = 1 AND RevokedAt IS NOT NULL)");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Token)
               .IsRequired()
               .HasMaxLength(ValidationConstants.RefreshToken.Lengths.TokenLength)
               .IsUnicode(true);
        
        builder.Property(r => r.Device)
               .HasMaxLength(ValidationConstants.RefreshToken.Lengths.DeviceLength)
               .IsUnicode(true);
        
        builder.Property(r => r.Browser)
               .HasMaxLength(ValidationConstants.RefreshToken.Lengths.BrowserLength)
               .IsUnicode(true);
        
        builder.Property(r => r.OperatingSystem)
               .HasMaxLength(ValidationConstants.RefreshToken.Lengths.OperatingSystemLength)
               .IsUnicode(true);
        
        builder.Property(r => r.IPAddress)
               .HasMaxLength(ValidationConstants.RefreshToken.Lengths.IPAddressLength)
               .IsUnicode(true);

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(r => r.IsRevoked)
               .HasDefaultValue(0);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(r => r.Token)
               .IsUnique()
               .HasDatabaseName(DbConstraints.RefreshTokens.UniqueActiveToken)
               .HasFilter("IsRevoked = 0");

        builder.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt })
               .HasDatabaseName(DbConstraints.RefreshTokens.IXUserActiveExpiration);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
