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

    // email confirmation method
    public async Task<Result<bool>> SendEmailConfirmation(string recipientEmail, string confirmationLink) {
        if(string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
           string.IsNullOrWhiteSpace(_settings.Username) ||
           string.IsNullOrWhiteSpace(_settings.Password) ||
           string.IsNullOrWhiteSpace(_settings.From)) {

            _logger.LogError("Email settings are not configured");
            return Result<bool>.Failure("Email service not configured", ErrorType.ExternalServiceError);
        }

        try {
            using MailMessage message = new() {
                From = new MailAddress(_settings.From),
                Subject = "Conferma registrazione account",
                Body = $"Ciao,\n\nconferma la tua registrazione cliccando sul link seguente:\n{confirmationLink}\n\nSe non hai richiesto questa registrazione, ignora questa email.",
                IsBodyHtml = false
            };
            message.To.Add(recipientEmail);

            using SmtpClient client = new(_settings.SmtpHost, _settings.SmtpPort) {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message);

            return Result<bool>.Success("Confirmation email sent");

        } catch(Exception ex) {
            _logger.LogError("Error during sending confirmation email: {Message}", ex.Message);
            return Result<bool>.Failure("Unable to send confirmation email", ErrorType.ExternalServiceError);
        }
    }

    // forgot password method
    public async Task<Result<bool>> SendForgotPasswordEmail(string recipientEmail, string token) {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
           string.IsNullOrWhiteSpace(_settings.Username) ||
           string.IsNullOrWhiteSpace(_settings.Password) ||
           string.IsNullOrWhiteSpace(_settings.From)) {

            _logger.LogError("Email settings are not configured");
            return Result<bool>.Failure("Email service not configured", ErrorType.ExternalServiceError);
        }

        try {
            using MailMessage message = new() {
                From = new MailAddress(_settings.From),
                Subject = "Richiesta reset password",
                Body = $"Ciao,\n\nCopia e incolla il seguente token nella pagina di reset password:\n{token}\n\nSe non hai richiesto il reset della password, ignora questa email.",
                IsBodyHtml = false
            };
            message.To.Add(recipientEmail);

            using SmtpClient client = new(_settings.SmtpHost, _settings.SmtpPort) {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message);

            return Result<bool>.Success("Forgot password email sent");

        } catch (Exception ex) {
            _logger.LogError("Error during sending forgot password email: {Message}", ex.Message);
            return Result<bool>.Failure("Unable to send forgot password email", ErrorType.ExternalServiceError);
        }
    }
}
