using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AuthService.Entities;
using AuthService.Services;

namespace AuthService.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _encryptionKey;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IConfiguration configuration) : base(options)
        {
            _encryptionKey = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException(
                    "Encryption key is not configured.");
        }

        public DbSet<UserEntity> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);

            var key = _encryptionKey;

            var requiredStringConverter = new ValueConverter<string, string>(
                value => EncryptionService.Encrypt(value, key),
                value => EncryptionService.Decrypt(value, key));

            var optionalStringConverter = new ValueConverter<string?, string?>(
                value => EncryptionService.Encrypt(value!, key),
                value => EncryptionService.Decrypt(value!, key));

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(u => u.FullName).HasConversion(requiredStringConverter);
                entity.Property(u => u.Biography).HasConversion(optionalStringConverter);
                entity.Property(u => u.PhoneNumber).HasConversion(optionalStringConverter);
                entity.Property(u => u.City).HasConversion(optionalStringConverter);
                entity.Property(u => u.Country).HasConversion(optionalStringConverter);
                entity.Property(u => u.Institution).HasConversion(optionalStringConverter);
                entity.Property(u => u.Career).HasConversion(optionalStringConverter);
                entity.Property(u => u.StudentId).HasConversion(optionalStringConverter);
            });
        }
    }
}