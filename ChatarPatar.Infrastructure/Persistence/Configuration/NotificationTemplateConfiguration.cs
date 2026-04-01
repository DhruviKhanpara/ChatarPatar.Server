using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates", t =>
        {
            t.HasCheckConstraint(
                "CK_NotificationTemplates_TemplateType",
                "TemplateType IN ('Email', 'Sms')");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(ValidationConstants.NotificationTemplate.Lengths.Name)
               .IsUnicode(true);

        builder.Property(t => t.TemplateType)
               .IsRequired()
               .HasMaxLength(ValidationConstants.NotificationTemplate.Lengths.TemplateType)
               .HasConversion<string>();

        builder.Property(t => t.SubjectText)
               .HasMaxLength(ValidationConstants.NotificationTemplate.Lengths.SubjectText)
               .IsUnicode(true);

        builder.Property(t => t.BodyText)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(t => t.IsActive)
               .HasDefaultValue(true);

        builder.Property(t => t.RowVersion)
               .IsRowVersion();

        // ----------------------------
        // Indexes
        // ----------------------------

        // Unique per (Name, TemplateType) — OrgInvite+Email and OrgInvite+Sms are both valid
        builder.HasIndex(t => new { t.Name, t.TemplateType })
               .IsUnique()
               .HasDatabaseName("UQ_NotificationTemplates_Name_Type");

        // Fast lookup by type — used when listing all email templates, etc.
        builder.HasIndex(t => t.TemplateType)
               .HasDatabaseName("IX_NotificationTemplates_TemplateType");

        // ----------------------------
        // Active-only query filter
        // ----------------------------
        builder.HasQueryFilter(t => t.IsActive);
    }
}
