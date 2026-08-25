using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThriftHub.Services
{
    public class PaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaystackService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // ============================================================
        // INITIALIZE PAYMENT
        // ============================================================

        public async Task<PaystackInitializeResponse?> InitializeTransaction(
            string email,
            decimal amount,
            string reference,
            string callbackUrl)
        {
            var secretKey =
                _configuration["Paystack:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new Exception(
                    "Paystack SecretKey is missing from appsettings.json."
                );
            }

            // Paystack expects amount in the smallest
            // currency unit.
            //
            // GH₵50 = 5000 pesewas
            // GH₵100 = 10000 pesewas
            // GH₵200 = 20000 pesewas

            var amountInPesewas =
                (int)Math.Round(amount * 100);

            var requestData = new
            {
                email = email,

                amount = amountInPesewas.ToString(),

                currency = "GHS",

                reference = reference,

                callback_url = callbackUrl,

                channels = new[]
                {
                    "card",
                    "mobile_money"
                }
            };

            var json =
                JsonSerializer.Serialize(requestData);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.paystack.co/transaction/initialize"
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    secretKey
                );

            request.Content = content;

            var response =
                await _httpClient.SendAsync(request);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paystack initialization failed: {responseContent}"
                );
            }

            var result =
                JsonSerializer.Deserialize<PaystackInitializeResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

            return result;
        }


        // ============================================================
        // VERIFY PAYMENT
        // ============================================================

        public async Task<PaystackVerifyResponse?> VerifyTransaction(
            string reference)
        {
            var secretKey =
                _configuration["Paystack:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new Exception(
                    "Paystack SecretKey is missing from appsettings.json."
                );
            }

            var url =
                $"https://api.paystack.co/transaction/verify/{reference}";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    secretKey
                );

            var response =
                await _httpClient.SendAsync(request);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paystack verification failed: {responseContent}"
                );
            }

            var result =
                JsonSerializer.Deserialize<PaystackVerifyResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

            return result;
        }
    }


    // ================================================================
    // INITIALIZE RESPONSE
    // ================================================================

    public class PaystackInitializeResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackInitializeData? Data { get; set; }
    }


    public class PaystackInitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string? AuthorizationUrl { get; set; }

        [JsonPropertyName("access_code")]
        public string? AccessCode { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }


    // ================================================================
    // VERIFY RESPONSE
    // ================================================================

    public class PaystackVerifyResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackVerifyData? Data { get; set; }
    }


    public class PaystackVerifyData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("gateway_response")]
        public string? GatewayResponse { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("customer")]
        public PaystackCustomer? Customer { get; set; }
    }


    // ================================================================
    // CUSTOMER
    // ================================================================

    public class PaystackCustomer
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("customer_code")]
        public string? CustomerCode { get; set; }
    }
}