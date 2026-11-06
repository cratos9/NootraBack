using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class ContactPreferenceConfiguration : IEntityTypeConfiguration<ContactPreference>
{
    public void Configure(EntityTypeBuilder<ContactPreference> builder)
    {
        builder.ToTable("contact_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(p => p.ContactId)
            .HasColumnName("contact_id")
            .IsRequired();

        builder.Property(p => p.IsPinned)
            .HasColumnName("is_pinned")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.IsAttached)
            .HasColumnName("is_attached")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(p => new { p.OwnerId, p.ContactId })
            .IsUnique()
            .HasDatabaseName("uq_contact_preferences_owner_contact");

        builder.HasIndex(p => p.OwnerId)
            .HasDatabaseName("ix_contact_preferences_owner_id");

        builder.HasOne(p => p.Contact)
            .WithMany()
            .HasForeignKey(p => p.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}