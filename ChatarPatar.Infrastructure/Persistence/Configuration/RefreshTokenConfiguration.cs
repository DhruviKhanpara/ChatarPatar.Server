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
                "CK_RefreshTokens_RevokeConsistency",
                "(IsRevoked = 0 AND RevokedAt IS NULL) OR " +
                "(IsRevoked = 1 AND RevokedAt IS NOT NULL)");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Token)
               .IsRequired()
               .HasMaxLength(FieldLengths.RefreshTokenFields.TokenLength)
               .IsUnicode(true);
        
        builder.Property(r => r.Device)
               .HasMaxLength(FieldLengths.RefreshTokenFields.DeviceLength)
               .IsUnicode(true);
        
        builder.Property(r => r.Browser)
               .HasMaxLength(FieldLengths.RefreshTokenFields.BrowserLength)
               .IsUnicode(true);
        
        builder.Property(r => r.OperatingSystem)
               .HasMaxLength(FieldLengths.RefreshTokenFields.OperatingSystemLength)
               .IsUnicode(true);
        
        builder.Property(r => r.IPAddress)
               .HasMaxLength(FieldLengths.RefreshTokenFields.IPAddressLength)
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
               .HasDatabaseName("UX_RefreshTokens_Token")
               .HasFilter("IsRevoked = 0");

        builder.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt })
               .HasDatabaseName("IX_RefreshToken_ActiveToken");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
