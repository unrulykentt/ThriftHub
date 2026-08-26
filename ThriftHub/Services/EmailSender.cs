using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var apiKey = ResolveApiKey();
            var senderEmail = ResolveSenderEmail();
            var senderName =
                _configuration["Resend:FromName"]
                ?? "ThriftHub";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Resend API key is not configured. " +
                    "Set Resend:ApiKey or RESEND_API_KEY on Render."
                );
            }

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Resend sender email is not configured. " +
                    "Set Resend:FromEmail to an address on your verified domain " +
                    "(for example noreply@thrifthubgh.com)."
                );
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(
                    "Recipient email address is empty."
                );
            }

            if (senderEmail.Contains(
                    "resend.dev",
                    StringComparison.OrdinalIgnoreCase) &&
                !_environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "onboarding@resend.dev can only send to your own Resend account email. " +
                    "Verify thrifthubgh.com on Resend and set Resend:FromEmail " +
                    "to noreply@thrifthubgh.com on Render."
                );
            }

            var requestBody = new
            {
                from = $"{senderName} <{senderEmail}>",
                to = new[] { email },
                subject = subject,
                html = message
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.resend.com/emails"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey
                );

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            using var response =
                await _httpClient.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Resend rejected email to {Recipient}. " +
                    "HTTP {StatusCode}. Response: {ResponseBody}",
                    email,
                    (int)response.StatusCode,
                    responseBody);

                throw new InvalidOperationException(
                    $"Resend email failed (HTTP {(int)response.StatusCode}). " +
                    DescribeResendError(responseBody)
                );
            }

            _logger.LogInformation(
                "Verification email sent to {Recipient} via Resend.",
                email);
        }

        private string? ResolveApiKey()
        {
            return
                _configuration["Resend:ApiKey"]
                ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
        }

        private string? ResolveSenderEmail()
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

        private static string DescribeResendError(string responseBody)
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
