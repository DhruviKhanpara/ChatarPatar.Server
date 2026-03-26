using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants", t =>
        {
            t.HasCheckConstraint(
                "CK_ConvParticipants_Role",
                "Role IN ('GroupAdmin','GroupMember')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.Role)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ConversationParticipantRoleEnum>(v))
               .HasMaxLength(ValidationConstants.Conversation.Lengths.Role)
               .HasDefaultValue(ConversationParticipantRoleEnum.GroupMember)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(p => p.JoinedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(p => p.HasLeft)
               .HasDefaultValue(false);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(p => p.ConversationId)
               .HasDatabaseName("IX_ConvParticipants_ConvId");

        builder.HasIndex(p => p.UserId)
               .HasDatabaseName("IX_ConvParticipants_UserId");

        builder.HasIndex(p => new { p.ConversationId, p.UserId })
               .IsUnique()
               .HasDatabaseName("UX_ConvParticipants_Active")
               .HasFilter("[HasLeft] = 0");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(p => p.Conversation)
               .WithMany(c => c.ConversationParticipants)
               .HasForeignKey(p => p.ConversationId)
               .HasConstraintName("FK_ConvParticipants_Conversation")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .HasConstraintName("FK_ConvParticipants_User")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AddedByUser)
               .WithMany()
               .HasForeignKey(p => p.AddedBy)
               .HasConstraintName("FK_ConvParticipants_AddedBy")
               .OnDelete(DeleteBehavior.Restrict);
    }
}