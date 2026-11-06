using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MensajeService.Data.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.BlockerId)
            .HasColumnName("blocker_id")
            .IsRequired();

        builder.Property(b => b.BlockedId)
            .HasColumnName("blocked_id")
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(b => new { b.BlockerId, b.BlockedId })
            .IsUnique()
            .HasDatabaseName("uq_user_blocks_pair");

        builder.HasIndex(b => b.BlockedId)
            .HasDatabaseName("ix_user_blocks_blocked_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_user_blocks_different_users",
                "blocker_id <> blocked_id"
            );
        });
    }
}