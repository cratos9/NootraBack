using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthService.Entities;

namespace AuthService.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(80);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            builder.Property(u => u.FullName).HasMaxLength(100);
            builder.Property(u => u.Biography).HasMaxLength(500);
            builder.Property(u => u.ProfilePictureUrl).HasMaxLength(255);
            builder.Property(u => u.PhoneNumber).HasMaxLength(20);
            builder.Property(u => u.Country).HasMaxLength(50);
            builder.Property(u => u.City).HasMaxLength(50);
            builder.Property(u => u.Institution).HasMaxLength(100);
            builder.Property(u => u.Career).HasMaxLength(100);
            builder.Property(u => u.StudentId).HasMaxLength(50);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.IsVerified).IsRequired();
            builder.Property(u => u.IsEmailVerified).IsRequired();
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.UpdatedAt);
            builder.Property(u => u.LastLogin);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();
        }
    }
}