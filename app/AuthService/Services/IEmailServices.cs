namespace AuthService.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipientEmail, string resetUrl);
}