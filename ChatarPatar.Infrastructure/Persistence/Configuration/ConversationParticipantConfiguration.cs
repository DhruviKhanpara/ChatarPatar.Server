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
                DbConstraints.ConversationParticipants.CKRole,
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
        // Unique Constraints
        // ----------------------------

        builder.HasIndex(a => new { a.ConversationId, a.UserId })
               .IsUnique()
               .HasDatabaseName(DbConstraints.ConversationParticipants.UniqueConversationUser);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(p => p.ConversationId)
               .HasDatabaseName(DbConstraints.ConversationParticipants.IXConversationId);

        builder.HasIndex(p => p.UserId)
               .HasDatabaseName(DbConstraints.ConversationParticipants.IXUserId);

        builder.HasIndex(p => new{ p.ConversationId, p.HasLeft })
               .HasDatabaseName(DbConstraints.ConversationParticipants.IXActiveConversation);

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(p => p.Conversation)
               .WithMany(c => c.ConversationParticipants)
               .HasForeignKey(p => p.ConversationId)
               .HasConstraintName(DbConstraints.ConversationParticipants.FKConversation)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .HasConstraintName(DbConstraints.ConversationParticipants.FKUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AddedByUser)
               .WithMany()
               .HasForeignKey(p => p.AddedBy)
               .HasConstraintName(DbConstraints.ConversationParticipants.FKAddedByUser)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RejoinedByUser)
               .WithMany()
               .HasForeignKey(p => p.RejoinedBy)
               .HasConstraintName(DbConstraints.ConversationParticipants.FKRejoinedByUser)
               .OnDelete(DeleteBehavior.Restrict);
    }
}