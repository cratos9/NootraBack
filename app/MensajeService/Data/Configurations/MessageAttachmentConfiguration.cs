using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("message_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(a => a.OriginalFilename)
            .HasColumnName("original_filename")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.StoredFilename)
            .HasColumnName("stored_filename")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.FilePath)
            .HasColumnName("file_path")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(a => a.MessageId)
            .HasDatabaseName("ix_message_attachments_message_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_message_attachments_positive_size",
                "file_size_bytes > 0"
            );
        });
    }
}