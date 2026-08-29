using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ThriftHub.Services
{
    public class WhatsAppCloudOtpSender : IWhatsAppOtpSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppCloudOtpSender> _logger;
        private readonly IWebHostEnvironment _environment;

        public WhatsAppCloudOtpSender(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WhatsAppCloudOtpSender> logger,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccessToken) &&
            !string.IsNullOrWhiteSpace(PhoneNumberId) &&
            !string.IsNullOrWhiteSpace(TemplateName);

        private string? AccessToken =>
            _configuration["WhatsApp:AccessToken"];

        private string? PhoneNumberId =>
            _configuration["WhatsApp:PhoneNumberId"];

        private string TemplateName =>
            _configuration["WhatsApp:TemplateName"]
            ?? "thrifthub_verify";

        private string TemplateLanguage =>
            _configuration["WhatsApp:TemplateLanguage"]
            ?? "en";

        public async Task SendVerificationCodeAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "WhatsApp OTP is not configured. Verification code for {Phone}: {Code}",
                    phoneNumber,
                    code);

                if (_environment.IsDevelopment())
                {
                    return;
                }

                throw new InvalidOperationException(
                    "WhatsApp verification is not configured.");
            }

            var recipient =
                phoneNumber
                    .Trim()
                    .TrimStart('+')
                    .Replace(" ", string.Empty);

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = recipient,
                type = "template",
                template = new
                {
                    name = TemplateName,
                    language = new
                    {
                        code = TemplateLanguage
                    },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = code
                                }
                            }
                        }
                    }
                }
            };

            var client =
                _httpClientFactory.CreateClient(
                    "WhatsAppCloud");

            var requestUri =
                $"https://graph.facebook.com/v22.0/{PhoneNumberId}/messages";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AccessToken);

            request.Content =
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

            using var response =
                await client.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogError(
                    "WhatsApp OTP failed for {Phone}. Status: {Status}. Response: {Body}",
                    phoneNumber,
                    response.StatusCode,
                    body);

                throw new InvalidOperationException(
                    "Unable to send WhatsApp verification code.");
            }
        }
    }
}
