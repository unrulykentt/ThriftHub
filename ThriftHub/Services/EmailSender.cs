using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ThriftHub.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailSender(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string message)
        {
            // =====================================================
            // RESEND CONFIGURATION
            // =====================================================

            var apiKey = _configuration["Resend:ApiKey"];

            var senderEmail =
                _configuration["Resend:FromEmail"]
                ?? "onboarding@resend.dev";

            var senderName =
                _configuration["Resend:FromName"]
                ?? "ThriftHub";


            // =====================================================
            // CHECK RESEND SETTINGS
            // =====================================================

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "ThriftHub Resend API key is not configured."
                );
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(
                    "Recipient email address is empty."
                );
            }


            // =====================================================
            // CREATE RESEND REQUEST
            // =====================================================

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


            // =====================================================
            // SEND EMAIL THROUGH RESEND
            // =====================================================

            try
            {
                using var response =
                    await _httpClient.SendAsync(request);

                var responseBody =
                    await response.Content.ReadAsStringAsync();


                // =================================================
                // CHECK RESEND RESPONSE
                // =================================================

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Resend email failed. " +
                        $"HTTP {(int)response.StatusCode}: " +
                        responseBody
                    );
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "ThriftHub could not send the verification email through Resend.",
                    ex
                );
            }
        }
    }
}