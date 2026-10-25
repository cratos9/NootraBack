using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthService.Services;

public sealed class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "Nootra";
    public string SenderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendPasswordResetAsync(string recipientEmail, string resetUrl)
    {
        var message = new MimeMessage();
        message.From.Add( new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "Reset password - Nootra";
        message.Body = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#222">
                    <h2>Reset password</h2>
                    <p>We received a request to change the password for your Nootra account.</p>
                    <p>
                        <a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}"style="background:#d7193f;color:white;padding:12px 20px;text-decoration:none;border-radius:6px;display:inline-block">
                            Reset password
                        </a>
                    </p>
                    <p>This link will expire in 15 minutes and can only be used once.</p>
                    <p>If you did not request this change, please ignore this email.</p>
                </div>
                """
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}