using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class OutboxMessagesConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(e => e.Id).HasName("PK_OutboxMessages");

        builder.Property(e => e.Type)
            .HasMaxLength(ValidationConstants.OutboxMessage.Lengths.Type);

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        builder.Property(x => x.RowVersion)
               .IsRowVersion();

        builder.Property(e => e.IsProcessed)
            .HasDefaultValue(false);

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(t => new { t.IsProcessed, t.NextAttemptAt })
               .HasDatabaseName("IX_OutboxMessages_Processing")
               .HasFilter("[IsDeleted] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedBy)
               .HasConstraintName("FK_OutboxMessages_CreatedBy")
               .OnDelete(DeleteBehavior.SetNull);

        // ----------------------------
        // Soft Delete Filter
        // ----------------------------

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
