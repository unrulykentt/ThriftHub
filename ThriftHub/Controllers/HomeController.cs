using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QrCodeService _qrCodeService;
        private readonly IConfiguration _configuration;

        public HomeController(
            ApplicationDbContext context,
            QrCodeService qrCodeService,
            IConfiguration configuration)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _configuration = configuration;
        }

        // ============================================================
        // HOME / MARKETPLACE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get all products that are still available
            var products = await _context.Products
                .Where(p => !p.IsSold)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.SellerDisplayNames =
                await UserPresentationHelper.LoadSellerDisplayNamesAsync(
                    _context,
                    products);

            return View(products);
        }


        // ============================================================
        // PRIVACY
        // ============================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }


        // ============================================================
        // QR CODE — scan to visit the site
        // ============================================================

        [HttpGet]
        public IActionResult Qr()
        {
            ViewBag.SiteUrl = GetPublicSiteUrl();

            return View();
        }


        [HttpGet]
        public IActionResult InstallApp()
        {
            return View();
        }


        [HttpGet]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public IActionResult QrCode(bool download = false)
        {
            var siteUrl = GetPublicSiteUrl();
            var png = _qrCodeService.CreatePng(siteUrl, pixelsPerModule: 14);

            if (download)
            {
                return File(
                    png,
                    "image/png",
                    "thrifthub-qr-code.png");
            }

            return File(png, "image/png");
        }


        private string GetPublicSiteUrl()
        {
            var configured =
                _configuration["ThriftHub:PublicUrl"];

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim().TrimEnd('/');
            }

            return $"{Request.Scheme}://{Request.Host}";
        }


        // ============================================================
        // ERROR
        // ============================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier
            });
        }
    }
}