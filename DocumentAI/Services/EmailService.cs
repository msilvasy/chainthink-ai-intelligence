using DocumentAI.Models;
using DocumentAI.Services.Interfaces;


namespace DocumentAI.Services
{
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(
            string to,
            string subject,
            string body,
            bool isHtml = false)
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.FromName,
                    _settings.FromEmail));

            message.To.Add(MailboxAddress.Parse(to));

            message.Subject = subject;

            message.Body = isHtml
                ? new TextPart("html") { Text = body }
                : new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();

            try
            {
                // For your server (port 465)
                await smtp.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.SslOnConnect);

                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password);

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Email sending failed. Host: {_settings.Host}, Port: {_settings.Port}",
                    ex);
            }
        }
    }
}
