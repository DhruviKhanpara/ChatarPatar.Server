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
                DbConstraints.Conversations.CKType,
                "Type IN ('Direct','Group')");

            t.HasCheckConstraint(
                DbConstraints.Conversations.CKDirectRule,
                @"(
                    (Type = 'Direct'
                        AND Name IS NULL
                        AND LogoFileId IS NULL
                        AND DirectParticipantAId IS NOT NULL
                        AND DirectParticipantBId IS NOT NULL
                        AND DirectParticipantAId < DirectParticipantBId)
                    OR
                    (Type = 'Group'
                        AND Name IS NOT NULL
                        AND DirectParticipantAId IS NULL
                        AND DirectParticipantBId IS NULL)
                )");
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

        builder.Property(c => c.LastMessageAt)
               .IsRequired(false);

        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique index
        // ----------------------------
        builder.HasIndex(c => new { c.DirectParticipantAId, c.DirectParticipantBId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.Conversations.UniqueDirectConversationParticipants)
               .HasFilter("[Type] = 'Direct'");

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(c => c.LastMessageAt)
               .HasDatabaseName(DbConstraints.Conversations.IXLastMessageAt);

        // ----------------------------
        // Relationships
        // ----------------------------
        builder.HasMany(c => c.Messages)
               .WithOne(m => m.Conversation)
               .HasForeignKey(m => m.ConversationId);

        builder.HasOne(c => c.LogoFile)
               .WithMany()
               .HasForeignKey(c => c.LogoFileId)
               .HasConstraintName(DbConstraints.Conversations.FKLogoFile)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DirectParticipantA)
               .WithMany()
               .HasForeignKey(c => c.DirectParticipantAId)
               .HasConstraintName(DbConstraints.Conversations.FKDirectParticipantA)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DirectParticipantB)
               .WithMany()
               .HasForeignKey(c => c.DirectParticipantBId)
               .HasConstraintName(DbConstraints.Conversations.FKDirectParticipantB)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedBy)
               .HasConstraintName(DbConstraints.Conversations.FKCreatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.UpdatedByUser)
               .WithMany()
               .HasForeignKey(c => c.UpdatedBy)
               .HasConstraintName(DbConstraints.Conversations.FKUpdatedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeletedByUser)
               .WithMany()
               .HasForeignKey(c => c.DeletedBy)
               .HasConstraintName(DbConstraints.Conversations.FKDeletedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        // ----------------------------
        // Soft Delete Global Filter
        // ----------------------------

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}