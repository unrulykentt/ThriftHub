namespace ThriftHub.Services
{
    public class SiteSeoService
    {
        public const string DefaultDescription =
            "ThriftHub is Ghana's student thrift marketplace. Buy and sell fashion, textbooks, electronics, hostel essentials, and more from verified sellers.";

        public const string DefaultKeywords =
            "ThriftHub, thrift Ghana, student marketplace, buy and sell Ghana, second hand fashion, textbooks Ghana, thrifthubgh";

        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SiteSeoService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public string SiteName =>
            "ThriftHub";

        public string PublicUrl
        {
            get
            {
                var configured =
                    _configuration["ThriftHub:PublicUrl"]?
                        .Trim()
                        .TrimEnd('/');

                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return configured;
                }

                var request =
                    _httpContextAccessor.HttpContext?.Request;

                if (request == null)
                {
                    return "https://thrifthubgh.com";
                }

                return $"{request.Scheme}://{request.Host}";
            }
        }

        public string? GoogleSiteVerification =>
            _configuration["ThriftHub:GoogleSiteVerification"]?
                .Trim();

        public string GetCanonicalUrl(
            string? relativePath = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                var request =
                    _httpContextAccessor.HttpContext?.Request;

                if (request == null)
                {
                    return PublicUrl;
                }

                return
                    $"{PublicUrl}{request.Path}{request.QueryString}";
            }

            relativePath =
                relativePath.StartsWith('/')
                    ? relativePath
                    : $"/{relativePath}";

            return $"{PublicUrl}{relativePath}";
        }

        public bool ShouldAllowIndexing(
            string? controller,
            string? action)
        {
            controller =
                controller?.Trim()
                ?? string.Empty;

            action =
                action?.Trim()
                ?? string.Empty;

            if (controller.Equals(
                    "Home",
                    StringComparison.OrdinalIgnoreCase))
            {
                return !action.Equals(
                    "Error",
                    StringComparison.OrdinalIgnoreCase);
            }

            if (controller.Equals(
                    "MarketPlace",
                    StringComparison.OrdinalIgnoreCase))
            {
                return action.Equals(
                           "Index",
                           StringComparison.OrdinalIgnoreCase)
                       || action.Equals(
                           "Details",
                           StringComparison.OrdinalIgnoreCase);
            }

            if (controller.Equals(
                    "Seller",
                    StringComparison.OrdinalIgnoreCase)
                && action.Equals(
                    "Profile",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
