using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels", t =>
        {
            t.HasCheckConstraint(
                "CK_Channels_Type",
                "Type IN ('Text','Announcement')");

            t.HasCheckConstraint(
                "CK_Channels_ArchiveState",
                "(IsArchived = 0 AND ArchivedAt IS NULL AND ArchivedBy IS NULL) OR (IsArchived = 1 AND ArchivedAt IS NOT NULL)"
    );
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(ValidationConstants.Channel.Lengths.Name)
               .IsUnicode(true);

        builder.Property(c => c.Description)
               .HasMaxLength(ValidationConstants.Channel.Lengths.Description)
               .IsUnicode(true);

        builder.Property(c => c.Type)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChannelTypeEnum>(v))
               .HasMaxLength(ValidationConstants.Channel.Lengths.Type)
               .HasDefaultValue(ChannelTypeEnum.Text)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(c => c.IsPrivate)
               .HasDefaultValue(false);

        builder.Property(c => c.IsArchived)
               .HasDefaultValue(false);

        builder.Property(c => c.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(c => c.TeamId)
               .HasDatabaseName("IX_Channels_TeamId");

        builder.HasIndex(c => new { c.TeamId, c.IsArchived })
               .HasDatabaseName("IX_Channels_Archived");

        builder.HasIndex(c => new { c.TeamId, c.Name })
                .IsUnique()
                .HasDatabaseName("UX_Channels_Name")
                .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(c => c.Team)
               .WithMany()
               .HasForeignKey(c => c.TeamId)
               .HasConstraintName("FK_Channels_Team")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Organization)
               .WithMany()
               .HasForeignKey(c => c.OrgId)
               .HasConstraintName("FK_Channels_Org")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ArchivedByUser)
               .WithMany()
               .HasForeignKey(c => c.ArchivedBy)
               .HasConstraintName("FK_Channels_Archiver")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedBy)
               .HasConstraintName("FK_Channels_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.UpdatedByUser)
               .WithMany()
               .HasForeignKey(c => c.UpdatedBy)
               .HasConstraintName("FK_Channels_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeletedByUser)
               .WithMany()
               .HasForeignKey(c => c.DeletedBy)
               .HasConstraintName("FK_Channels_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}