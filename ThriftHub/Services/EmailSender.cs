using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ThriftHub.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IHostEnvironment environment,
            ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Resend");
            _environment = environment;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string message)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(
                    "Recipient email address is empty.");
            }

            var resendError =
                await TrySendViaResendAsync(
                    email,
                    subject,
                    message);

            if (resendError == null)
            {
                return;
            }

            _logger.LogWarning(
                "Resend could not send email to {Recipient}: {Error}. Trying SMTP fallback.",
                email,
                resendError);

            var smtpError =
                await TrySendViaSmtpAsync(
                    email,
                    subject,
                    message);

            if (smtpError == null)
            {
                return;
            }

            _logger.LogError(
                "All email providers failed for {Recipient}. Resend: {ResendError}. SMTP: {SmtpError}",
                email,
                resendError,
                smtpError);

            throw new InvalidOperationException(
                "Email could not be sent. Please try again later.");
        }

        private async Task<string?> TrySendViaResendAsync(
            string email,
            string subject,
            string message)
        {
            var apiKey =
                ResolveApiKey();

            var senderEmail =
                ResolveResendSenderEmail();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Resend API key is not configured.";
            }

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                return "Resend sender email is not configured.";
            }

            if (
                senderEmail.Contains(
                    "resend.dev",
                    StringComparison.OrdinalIgnoreCase) &&
                !_environment.IsDevelopment())
            {
                return
                    "onboarding@resend.dev cannot send to users in production.";
            }

            var senderName =
                _configuration["Resend:FromName"]
                ?? "ThriftHub";

            var requestBody =
                new
                {
                    from = $"{senderName} <{senderEmail}>",
                    to = new[] { email },
                    subject = subject,
                    html = message
                };

            var json =
                JsonSerializer.Serialize(requestBody);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.resend.com/emails");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            try
            {
                using var response =
                    await _httpClient.SendAsync(request);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return
                        $"HTTP {(int)response.StatusCode}: {DescribeResendError(responseBody)}";
                }

                _logger.LogInformation(
                    "Email sent to {Recipient} via Resend.",
                    email);

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<string?> TrySendViaSmtpAsync(
            string email,
            string subject,
            string message)
        {
            var senderEmail =
                _configuration["EmailSettings:SenderEmail"]
                ?? _configuration["Gmail:Email"];

            var senderPassword =
                _configuration["EmailSettings:SenderPassword"]
                ?? _configuration["Gmail:AppPassword"];

            var senderName =
                _configuration["EmailSettings:SenderName"]
                ?? "ThriftHub";

            var smtpServer =
                _configuration["EmailSettings:SmtpServer"]
                ?? "smtp.gmail.com";

            var smtpPort =
                int.TryParse(
                    _configuration["EmailSettings:SmtpPort"],
                    out var port)
                    ? port
                    : 587;

            if (
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(senderPassword))
            {
                return "SMTP sender email or password is not configured.";
            }

            try
            {
                var mimeMessage =
                    new MimeMessage();

                mimeMessage.From.Add(
                    new MailboxAddress(
                        senderName,
                        senderEmail));

                mimeMessage.To.Add(
                    MailboxAddress.Parse(email));

                mimeMessage.Subject =
                    subject;

                mimeMessage.Body =
                    new TextPart("html")
                    {
                        Text = message
                    };

                using var smtp =
                    new SmtpClient();

                await smtp.ConnectAsync(
                    smtpServer,
                    smtpPort,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    senderEmail,
                    senderPassword);

                await smtp.SendAsync(mimeMessage);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email sent to {Recipient} via SMTP ({SmtpServer}).",
                    email,
                    smtpServer);

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string? ResolveApiKey()
        {
            return
                _configuration["Resend:ApiKey"]
                ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
        }

        private string? ResolveResendSenderEmail()
        {
            var configuredEmail =
                _configuration["Resend:FromEmail"];

            if (!string.IsNullOrWhiteSpace(configuredEmail))
            {
                return configuredEmail.Trim();
            }

            if (_environment.IsDevelopment())
            {
                return "onboarding@resend.dev";
            }

            return null;
        }

        private static string DescribeResendError(
            string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return "Resend returned an empty error response.";
            }

            try
            {
                using var document =
                    JsonDocument.Parse(responseBody);

                if (document.RootElement.TryGetProperty(
                        "message",
                        out var messageElement))
                {
                    return messageElement.GetString()
                        ?? responseBody;
                }
            }
            catch (JsonException)
            {
            }

            return responseBody;
        }
    }
}
