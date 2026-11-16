using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using MensajeService.Data.Converters;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.SenderId)
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .HasConversion<EncryptedMessageContentConverter>();

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.IsEdited)
            .HasColumnName("is_edited")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt, m.Id })
            .HasDatabaseName("ix_messages_conversation_created_at");

        builder.HasIndex(m => m.SenderId)
            .HasDatabaseName("ix_messages_sender_id");

        builder.HasMany(m => m.Receipts)
            .WithOne(r => r.Message)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Conversation)
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_messages_valid_type",
                "type IN ('Text', 'Image', 'File')"
            );
        });
    }
}