using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        builder.ToTable("ChannelMembers", t =>
        {
            t.HasCheckConstraint(
                "CK_ChannelMembers_Role",
                "Role IN ('ChannelModerator','ChannelMember','ChannelReadOnly')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.Role)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChannelRoleEnum>(v))
               .HasMaxLength(FieldLengths.ChannelFields.Role)
               .HasDefaultValue(ChannelRoleEnum.ChannelMember)
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

        builder.HasIndex(m => m.ChannelId)
               .HasDatabaseName("IX_ChannelMembers_ChannelId");

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName("IX_ChannelMembers_UserId");

        builder.HasIndex(m => new { m.ChannelId, m.UserId })
               .IsUnique()
               .HasDatabaseName("UX_ChannelMembers_Active")
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Channel)
               .WithMany(c => c.ChannelMembers)
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName("FK_ChannelMembers_Channel")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .HasConstraintName("FK_ChannelMembers_User")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AddedByUser)
               .WithMany()
               .HasForeignKey(m => m.AddedByUserId)
               .HasConstraintName("FK_ChannelMembers_AddedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedBy)
               .HasConstraintName("FK_ChannelMembers_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UpdatedByUser)
               .WithMany()
               .HasForeignKey(m => m.UpdatedBy)
               .HasConstraintName("FK_ChannelMembers_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName("FK_ChannelMembers_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}