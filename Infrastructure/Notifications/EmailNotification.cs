using Microsoft.Extensions.Options;
using OptimusPrime.Core.Network;
using OptimusPrime.Core.Settings;
using OptimusPrime.Core.Utils;
using System.Net;
using System.Net.Mail;

namespace OptimusPrime.Infrastructure.Notifications;

public class EmailNotification(
    IOptions<EmailSettings> options,
    ILogger<EmailNotification> logger) {

    private readonly EmailSettings _settings = options.Value;
    private readonly ILogger<EmailNotification> _logger = logger;

    // ==========================
    // Email Confirmation
    // ==========================
    public async Task<Result<bool>> SendEmailConfirmation(string recipientEmail, string confirmationLink) {
        if(string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.Username)) {
            return Result<bool>.Failure("Email service not configured", ErrorType.ExternalServiceError);
        }

        try {
            string title = "Confirm your registration";
            string content = $@"
                <p>Hello,</p>
                <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
                <p style='text-align: center; margin: 30px 0;'>
                    <a href='{confirmationLink}' class='btn'>Confirm Email</a>
                </p>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='word-break: break-all; font-size: 12px; color: #007bff;'>{confirmationLink}</p>";

            using MailMessage message = new() {
                From = new MailAddress(_settings.From),
                Subject = "Confirm your account registration",
                Body = GetHtmlTemplate(title, content),
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            using SmtpClient client = GetSmtpClient();
            await client.SendMailAsync(message);

            return Result<bool>.Success("Confirmation email sent");

        } catch(Exception ex) {
            _logger.LogError("Error during sending confirmation email: {Message}", ex.Message);
            return Result<bool>.Failure("Unable to send confirmation email", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Forgot Password
    // ==========================
    public async Task<Result<bool>> SendForgotPasswordEmail(string recipientEmail, string token) {
        if(string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.Username)) {
            return Result<bool>.Failure("Email service not configured", ErrorType.ExternalServiceError);
        }

        try {
            string title = "Password Reset Request";
            string content = $@"
                <p>Hello,</p>
                <p>We received a request to reset the password for your account. Copy and paste the following security token on the reset page:</p>
                <div class='code-box'>{token}</div>
                <p>This token will expire in 1 hour.</p>";

            using MailMessage message = new() {
                From = new MailAddress(_settings.From),
                Subject = "Password Reset Request",
                Body = GetHtmlTemplate(title, content),
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            using SmtpClient client = GetSmtpClient();
            await client.SendMailAsync(message);

            return Result<bool>.Success("Forgot password email sent");

        } catch(Exception ex) {
            _logger.LogError("Error during sending forgot password email: {Message}", ex.Message);
            return Result<bool>.Failure("Unable to send forgot password email", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Device Sync / OTP
    // ==========================
    public async Task<Result<bool>> SendDeviceSyncEmail(string recipientEmail, string otpCode, string reason) {
        if(string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.Username)) {
            return Result<bool>.Failure("Email service not configured", ErrorType.ExternalServiceError);
        }

        try {
            string motivo = reason == "new device"
                ? "We detected a login attempt from a new device."
                : "Your usual device lost its security synchronization (e.g., browser cache was cleared).";

            string title = "Device Security Verification";
            string content = $@"
                <p>Hello,</p>
                <p>{motivo}</p>
                <p>To authorize this device and complete your login, please enter the following One-Time Password (OTP):</p>
                <div class='code-box'>{otpCode}</div>
                <p class='alert'><strong>Security Alert:</strong> If you did not attempt to log in, your password has been compromised. Please reset it immediately.</p>";

            using MailMessage message = new() {
                From = new MailAddress(_settings.From),
                Subject = "Device Security Code",
                Body = GetHtmlTemplate(title, content),
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            using SmtpClient client = GetSmtpClient();
            await client.SendMailAsync(message);

            return Result<bool>.Success("Device sync email sent");

        } catch(Exception ex) {
            _logger.LogError("Error during sending device sync email: {Message}", ex.Message);
            return Result<bool>.Failure("Unable to send device sync email", ErrorType.ExternalServiceError);
        }
    }

    // ==========================
    // Helpers
    // ==========================

    private SmtpClient GetSmtpClient() {
        return new SmtpClient(_settings.SmtpHost, _settings.SmtpPort) {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };
    }

    private string GetHtmlTemplate(string title, string content) {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333333; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
                .header {{ border-bottom: 2px solid #f0f0f0; padding-bottom: 20px; margin-bottom: 20px; text-align: center; }}
                h2 {{ color: #2c3e50; margin: 0; font-size: 24px; }}
                p {{ line-height: 1.6; font-size: 15px; color: #555555; margin: 0 0 15px 0; }}
                .btn {{ display: inline-block; background-color: #007bff; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; }}
                .code-box {{ background-color: #f8f9fa; border: 1px dashed #ced4da; border-radius: 6px; padding: 20px; font-size: 24px; font-weight: bold; text-align: center; letter-spacing: 4px; color: #212529; margin: 25px 0; word-break: break-all; }}
                .footer {{ font-size: 12px; color: #999999; border-top: 1px solid #f0f0f0; padding-top: 20px; margin-top: 30px; text-align: center; }}
                .alert {{ color: #dc3545; background-color: #f8d7da; padding: 15px; border-radius: 6px; border-left: 4px solid #dc3545; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>{title}</h2>
                </div>
                {content}
                <div class='footer'>
                    <p>If you did not request this action, please ignore this email.</p>
                    <p>&copy; {DateTime.UtcNow.Year} OptimusPrime. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}