using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ThriftHub.Services
{
    public class GmailEmailService
    {
        private readonly IConfiguration _configuration;

        public GmailEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationCodeAsync(
            string recipientEmail,
            string verificationCode)
        {
            // Get Gmail settings from appsettings.json
            var senderEmail = _configuration["Gmail:Email"];
            var appPassword = _configuration["Gmail:AppPassword"];

            // Check that the settings exist
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new Exception("Gmail:Email is missing in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(appPassword))
            {
                throw new Exception("Gmail:AppPassword is missing in appsettings.json.");
            }

            // Create the email
            var message = new MimeMessage();

            // Sender
            message.From.Add(
                new MailboxAddress("ThriftHub", senderEmail)
            );

            // Recipient
            message.To.Add(
                new MailboxAddress("", recipientEmail)
            );

            // Subject
            message.Subject = "Your ThriftHub Verification Code";

            // Email body
            message.Body = new TextPart("plain")
            {
                Text =
                    $"Hello,\n\n" +
                    $"Your ThriftHub verification code is: {verificationCode}\n\n" +
                    $"This code will be used to verify your email address.\n\n" +
                    $"If you did not create a ThriftHub account, you can ignore this email.\n\n" +
                    $"Regards,\n" +
                    $"ThriftHub Team"
            };

            // Connect to Gmail SMTP server
            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                "smtp.gmail.com",
                587,
                SecureSocketOptions.StartTls
            );

            // Login using Gmail + App Password
            await smtp.AuthenticateAsync(
                senderEmail,
                appPassword
            );

            // Send email
            await smtp.SendAsync(message);

            // Close connection
            await smtp.DisconnectAsync(true);
        }
    }
}