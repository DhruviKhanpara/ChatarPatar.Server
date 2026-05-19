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
                DbConstraints.ChannelMembers.CKRole,
                "Role IN ('ChannelModerator','ChannelMember','ChannelReadOnly')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(m => m.Role)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChannelRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Channel.Lengths.Role)
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
               .HasDatabaseName(DbConstraints.ChannelMembers.IXChannelId);

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName(DbConstraints.ChannelMembers.IXUserId);

        builder.HasIndex(m => new { m.ChannelId, m.UserId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.ChannelMembers.UniqueActiveChannelMembers)
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(m => m.Channel)
               .WithMany(c => c.ChannelMembers)
               .HasForeignKey(m => m.ChannelId)
               .HasConstraintName(DbConstraints.ChannelMembers.FKChannel)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .HasConstraintName(DbConstraints.ChannelMembers.FKUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AddedByUser)
               .WithMany()
               .HasForeignKey(m => m.AddedByUserId)
               .HasConstraintName(DbConstraints.ChannelMembers.FKAddedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
               .WithMany()
               .HasForeignKey(m => m.CreatedBy)
               .HasConstraintName(DbConstraints.ChannelMembers.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UpdatedByUser)
               .WithMany()
               .HasForeignKey(m => m.UpdatedBy)
               .HasConstraintName(DbConstraints.ChannelMembers.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DeletedByUser)
               .WithMany()
               .HasForeignKey(m => m.DeletedBy)
               .HasConstraintName(DbConstraints.ChannelMembers.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}