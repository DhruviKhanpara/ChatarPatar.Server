using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations", t =>
        {
            t.HasCheckConstraint(
                "CK_Conversations_Type",
                "Type IN ('Direct','Group')");

            t.HasCheckConstraint(
                "CK_Conversations_NameRule",
                "(Type = 'Direct' AND Name IS NULL) OR (Type = 'Group' AND Name IS NOT NULL)");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.Type)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ConversationTypeEnum>(v))
               .HasMaxLength(ValidationConstants.Conversation.Lengths.Type)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(c => c.Name)
               .HasMaxLength(ValidationConstants.Conversation.Lengths.Name)
               .IsUnicode(true);

        builder.Property(c => c.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(c => c.LogoFile)
               .WithMany()
               .HasForeignKey(c => c.LogoFileId)
               .HasConstraintName("FK_Conversations_Logo")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedBy)
               .HasConstraintName("FK_Conversations_CreatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.UpdatedByUser)
               .WithMany()
               .HasForeignKey(c => c.UpdatedBy)
               .HasConstraintName("FK_Conversations_UpdatedBy")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeletedByUser)
               .WithMany()
               .HasForeignKey(c => c.DeletedBy)
               .HasConstraintName("FK_Conversations_DeletedBy")
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}