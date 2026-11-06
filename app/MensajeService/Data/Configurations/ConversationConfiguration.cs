using MensajeService.Models;
using MensajeService.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.DirectKey)
            .HasColumnName("direct_key")
            .HasMaxLength(73);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(c => c.DirectKey)
            .IsUnique()
            .HasDatabaseName("uq_conversations_direct_key")
            .HasFilter("\"type\" = 'Direct'");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_conversations_valid_type",
                "type IN ('Direct', 'Group')"
            );

            table.HasCheckConstraint(
                "ck_conversations_direct_key",
                "(type = 'Direct' AND direct_key IS NOT NULL) OR " +
                "(type = 'Group' AND direct_key IS NULL)"
            );
        });

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Conversation)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}