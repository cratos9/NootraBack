using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class ContactReportConfiguration : IEntityTypeConfiguration<ContactReport>
{
    public void Configure(EntityTypeBuilder<ContactReport> builder)
    {
        builder.ToTable("contact_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.ReporterId)
            .HasColumnName("reporter_id")
            .IsRequired();

        builder.Property(r => r.ReportedId)
            .HasColumnName("reported_id")
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(r => r.ReportedId)
            .HasDatabaseName("ix_contact_reports_reported_id");

        builder.HasIndex(r => r.ReporterId)
            .HasDatabaseName("ix_contact_reports_reporter_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_contact_reports_different_users",
                "reporter_id <> reported_id"
            );

            table.HasCheckConstraint(
                "ck_contact_reports_valid_status",
                "status IN ('Pending', 'Reviewed', 'Resolved', 'Dismissed')"
            );
        });
    }
}