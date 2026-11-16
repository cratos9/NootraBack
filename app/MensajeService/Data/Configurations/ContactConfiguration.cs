using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.RequesterId)
            .HasColumnName("requester_id")
            .IsRequired();

        builder.Property(c => c.AddresseeId)
            .HasColumnName("addressee_id")
            .IsRequired();

        builder.Property(c => c.UserLowId)
            .HasColumnName("user_low_id")
            .IsRequired();

        builder.Property(c => c.UserHighId)
            .HasColumnName("user_high_id")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(c => new { c.UserLowId, c.UserHighId })
            .IsUnique()
            .HasDatabaseName("uq_contacts_user_pair");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_contacts_different_users",
                "requester_id <> addressee_id"
            );

            table.HasCheckConstraint(
                "ck_contacts_valid_pair",
                "user_low_id < user_high_id AND " +
                "user_low_id = LEAST(requester_id, addressee_id) AND " +
                "user_high_id = GREATEST(requester_id, addressee_id)"
            );

            table.HasCheckConstraint(
                "ck_contacts_valid_status",
                "status IN ('Pending', 'Accepted', 'Rejected', 'Cancelled')"
            );
        });
    }
}