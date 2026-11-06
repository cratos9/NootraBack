using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("conversation_participants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.LeftAt)
            .HasColumnName("left_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.ConversationId, p.UserId })
            .IsUnique()
            .HasDatabaseName("uq_conversation_participants_pair");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("ix_conversation_participants_user_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_conversation_participants_valid_dates",
                "left_at IS NULL OR left_at >= joined_at"
            );
        });
    }
}