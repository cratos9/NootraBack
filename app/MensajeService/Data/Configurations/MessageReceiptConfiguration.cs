using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class MessageReceiptConfiguration : IEntityTypeConfiguration<MessageReceipt>
{
    public void Configure(EntityTypeBuilder<MessageReceipt> builder)
    {
        builder.ToTable("message_receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(r => r.RecipientId)
            .HasColumnName("recipient_id")
            .IsRequired();

        builder.Property(r => r.DeliveredAt)
            .HasColumnName("delivered_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.MessageId, r.RecipientId })
            .IsUnique()
            .HasDatabaseName("uq_message_receipts_pair");

        builder.HasIndex(r => r.RecipientId)
            .HasDatabaseName("ix_message_receipts_recipient_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_message_receipts_read_requires_delivery",
                "read_at IS NULL OR delivered_at IS NOT NULL"
            );
        });
    }
}