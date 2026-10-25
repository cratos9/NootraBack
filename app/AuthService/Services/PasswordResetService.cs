using System.Security.Cryptography;
using System.Text;
using AuthService.Data;
using AuthService.Dtos;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(AppDbContext context, IEmailService emailService, IOptions<EmailSettings> emailOptions, IConfiguration configuration, ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _emailSettings = emailOptions.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RequestResetAsync(PasswordResetRequestDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u =>u.Email == email && u.IsActive);

        if (user is null)
            return;

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var expirationMinutes = _configuration.GetValue<int?>("PasswordReset:TokenExpirationMinutes") ?? 15;
        user.PasswordResetTokenHash = tokenHash;
        user.PasswordResetTokenExpiryAt =DateTime.UtcNow.AddMinutes(expirationMinutes);
        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["PasswordReset:FrontendUrl"];
        if (string.IsNullOrWhiteSpace(frontendUrl))
            throw new InvalidOperationException("Is not configured PasswordReset:FrontendUrl.");

        var separator = frontendUrl.Contains('?') ? "&" : "?";

        var resetUrl = $"{frontendUrl}{separator}email={Uri.EscapeDataString(user.Email)}" + $"&token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, resetUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Is not possible to send the recovery email.");
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiryAt = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ResetPasswordAsync(PasswordResetDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user is null ||string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) || user.PasswordResetTokenExpiryAt is null || user.PasswordResetTokenExpiryAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("The password reset link is invalid or has expired.");
        }

        var receivedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Token)));
        var expectedHash = Convert.FromHexString(user.PasswordResetTokenHash);
        var receivedHashBytes = Convert.FromHexString(receivedHash);

        if (!CryptographicOperations.FixedTimeEquals(expectedHash, receivedHashBytes))
        {
            throw new ArgumentException("The password reset link is invalid or has expired.");
        }

        if (dto.NewPassword.Length < 8 || !dto.NewPassword.Any(char.IsUpper) || !dto.NewPassword.Any(char.IsLower) || !dto.NewPassword.Any(char.IsDigit))
        {
            throw new ArgumentException("The password must be at least 8 characters long, " + "contain an uppercase letter, a lowercase letter, and a number.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiryAt = null;
        user.RefreshToken = null;
        user.RefreshTokenExpiryAt = null;

        await _context.SaveChangesAsync();
    }
}