using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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