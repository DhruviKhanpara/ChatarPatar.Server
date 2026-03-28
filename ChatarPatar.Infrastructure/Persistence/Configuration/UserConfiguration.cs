using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required fields
        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(ValidationConstants.User.Lengths.Email)
               .IsUnicode(false);

        builder.Property(u => u.Username)
               .IsRequired()
               .HasMaxLength(ValidationConstants.User.Lengths.Username)
               .IsUnicode(true);

        builder.Property(u => u.Name)
               .IsRequired()
               .HasMaxLength(ValidationConstants.User.Lengths.Name)
               .IsUnicode(true);

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(ValidationConstants.User.Lengths.PasswordHash)
               .IsUnicode(true);

        // Optional fields
        builder.Property(u => u.Bio)
               .HasMaxLength(ValidationConstants.User.Lengths.Bio)
               .IsUnicode(true);

        // Boolean defaults
        builder.Property(u => u.IsEmailVerified)
               .HasDefaultValue(false);

        builder.Property(u => u.IsDeleted)
               .HasDefaultValue(false);

        // DateTime defaults
        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.UpdatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // Unique Constraints
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("UQ_Users_Email");

        builder.HasIndex(u => u.Username)
               .IsUnique()
               .HasDatabaseName("UQ_Users_Username");

        // Foreign Key: AvatarFile
        builder.HasOne(u => u.AvatarFile)
               .WithMany()
               .HasForeignKey(u => u.AvatarFileId)
               .HasConstraintName("FK_Users_AvatarFile")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.DeletedByUser)
                .WithMany()
                .HasForeignKey(u => u.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft delete filter
        // ----------------------------

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}