using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ThriftHub.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string message)
        {
            var smtpServer =
                _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";

            var smtpPort =
                int.Parse(
                    _configuration["EmailSettings:SmtpPort"]
                    ?? "587"
                );

            var senderEmail =
                _configuration["EmailSettings:SenderEmail"];

            var senderPassword =
                _configuration["EmailSettings:SenderPassword"];

            var senderName =
                _configuration["EmailSettings:SenderName"]
                ?? "ThriftHub";

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                throw new Exception("EmailSettings:SenderEmail or SenderPassword is missing in configuration.");
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            mimeMessage.To.Add(new MailboxAddress("", email));
            mimeMessage.Subject = subject;

            mimeMessage.Body = new TextPart("html")
            {
                Text = message
            };

            using var client = new SmtpClient();

            var secureSocketOption = SecureSocketOptions.StartTls;
            if (smtpPort == 465)
            {
                secureSocketOption = SecureSocketOptions.SslOnConnect;
            }

            await client.ConnectAsync(smtpServer, smtpPort, secureSocketOption);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);
        }
    }
}