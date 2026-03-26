using ChatarPatar.Common.Consts;
using ChatarPatar.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatarPatar.Infrastructure.Persistence.Configuration;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Emoji)
               .HasMaxLength(ValidationConstants.Message.Lengths.Emoji)
               .IsRequired()
               .IsUnicode(true);

        builder.Property(r => r.CreatedAt)
               .HasDefaultValueSql("SYSUTCDATETIME()");

        // ----------------------------
        // Unique Constraint
        // ----------------------------

        builder.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji })
               .IsUnique()
               .HasDatabaseName("UQ_MessageReactions");

        builder.HasIndex(r => r.MessageId)
               .HasDatabaseName("IX_MessageReactions_MessageId");

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasOne(r => r.Message)
               .WithMany()
               .HasForeignKey(r => r.MessageId)
               .HasConstraintName("FK_MessageReactions_Message")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .HasConstraintName("FK_MessageReactions_User")
               .OnDelete(DeleteBehavior.Restrict);
    }
}