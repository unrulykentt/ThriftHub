using System.Net;
using System.Net.Mail;

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
                _configuration["EmailSettings:SmtpServer"];

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

            using var mailMessage = new MailMessage();

            mailMessage.From =
                new MailAddress(
                    senderEmail!,
                    senderName
                );

            mailMessage.To.Add(email);

            mailMessage.Subject = subject;

            mailMessage.Body = message;

            mailMessage.IsBodyHtml = true;

            using var smtpClient =
                new SmtpClient(
                    smtpServer,
                    smtpPort
                );

            smtpClient.EnableSsl = true;

            smtpClient.Credentials =
                new NetworkCredential(
                    senderEmail,
                    senderPassword
                );

            await smtpClient.SendMailAsync(
                mailMessage
            );
        }
    }
}