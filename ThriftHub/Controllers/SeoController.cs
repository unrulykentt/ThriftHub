using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    public class SeoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SiteSeoService _seo;

        public SeoController(
            ApplicationDbContext context,
            SiteSeoService seo)
        {
            _context = context;
            _seo = seo;
        }

        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 86400)]
        public ContentResult Robots()
        {
            var sitemapUrl =
                _seo.GetCanonicalUrl("/sitemap.xml");

            var robots =
                $"""
                User-agent: *
                Allow: /
                Disallow: /Admin/
                Disallow: /Dashboard/
                Disallow: /Account/
                Disallow: /Messages/
                Disallow: /AdminVerification/
                Disallow: /Products/
                Disallow: /Sell/
                Disallow: /Subscription/
                Disallow: /Safety/
                Disallow: /Order/
                Disallow: /Verification/
                Disallow: /Favorites/

                Sitemap: {sitemapUrl}
                """;

            return Content(
                robots,
                "text/plain",
                Encoding.UTF8);
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<ContentResult> Sitemap()
        {
            XNamespace ns =
                "http://www.sitemaps.org/schemas/sitemap/0.9";

            var urls =
                new List<(string Path, DateTime? LastModified)>
                {
                    ("/", DateTime.UtcNow),
                    ("/MarketPlace", DateTime.UtcNow),
                    ("/Home/Qr", DateTime.UtcNow),
                    ("/Home/InstallApp", DateTime.UtcNow),
                    ("/Home/Privacy", DateTime.UtcNow),
                    ("/Account/Register", DateTime.UtcNow),
                    ("/Account/Login", DateTime.UtcNow)
                };

            var products =
                await _context.Products
                    .AsNoTracking()
                    .Where(product => !product.IsSold)
                    .OrderByDescending(product => product.CreatedAt)
                    .Select(product => new
                    {
                        product.Id,
                        product.CreatedAt
                    })
                    .Take(5000)
                    .ToListAsync();

            foreach (var product in products)
            {
                urls.Add(
                    ($"/MarketPlace/Details/{product.Id}",
                     product.CreatedAt));
            }

            var sellerIds =
                await _context.Products
                    .AsNoTracking()
                    .Where(product =>
                        !product.IsSold &&
                        !string.IsNullOrWhiteSpace(product.SellerId))
                    .Select(product => product.SellerId!)
                    .Distinct()
                    .Take(2000)
                    .ToListAsync();

            foreach (var sellerId in sellerIds)
            {
                urls.Add(
                    ($"/Seller/Profile/{sellerId}",
                     DateTime.UtcNow));
            }

            var urlElements =
                urls
                    .DistinctBy(entry => entry.Path)
                    .Select(entry =>
                    {
                        var element =
                            new XElement(
                                ns + "url",
                                new XElement(
                                    ns + "loc",
                                    _seo.GetCanonicalUrl(entry.Path)));

                        if (entry.LastModified.HasValue)
                        {
                            element.Add(
                                new XElement(
                                    ns + "lastmod",
                                    entry.LastModified.Value
                                        .ToUniversalTime()
                                        .ToString("yyyy-MM-dd")));
                        }

                        return element;
                    });

            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(
                        ns + "urlset",
                        urlElements));

            return Content(
                document.ToString(),
                "application/xml",
                Encoding.UTF8);
        }
    }
}
