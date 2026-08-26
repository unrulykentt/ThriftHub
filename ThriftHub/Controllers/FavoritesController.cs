using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // WISHLIST PAGE
        // URL: /Favorites
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var products = await _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Join(
                    _context.Products,
                    favorite => favorite.ProductId,
                    product => product.Id,
                    (favorite, product) => product
                )
                .Where(p => !p.IsSold)
                .ToListAsync();

            ViewBag.WishlistCount = products.Count;

            return View("Wishlist", products);
        }


        // =========================================================
        // ADD / REMOVE WISHLIST
        // URL: /Favorites/Toggle
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId &&
                    f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        isFavorite = false,
                        message =
                            "Item removed from your wishlist."
                    });
                }

                TempData["WishlistMessage"] =
                    "Item removed from your wishlist.";
            }
            else
            {
                var newFavorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Favorites.Add(newFavorite);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        isFavorite = true,
                        message =
                            "Item added to your wishlist."
                    });
                }

                TempData["WishlistMessage"] =
                    "Item added to your wishlist.";
            }

            return RedirectToAction(
                "Index",
                "MarketPlace");
        }


        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"]
                       .ToString()
                       .Equals(
                           "XMLHttpRequest",
                           StringComparison.OrdinalIgnoreCase)
                   ||
                   Request.Headers.Accept
                       .ToString()
                       .Contains(
                           "application/json",
                           StringComparison.OrdinalIgnoreCase);
        }


        // =========================================================
        // REMOVE FROM WISHLIST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId &&
                    f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CHECK IF PRODUCT IS FAVORITE
        // URL: /Favorites/IsFavorite?productId=1
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> IsFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    isFavorite = false
                });
            }

            var isFavorite = await _context.Favorites
                .AnyAsync(f =>
                    f.UserId == userId &&
                    f.ProductId == productId);

            return Json(new
            {
                isFavorite
            });
        }


        // =========================================================
        // WISHLIST COUNT
        // URL: /Favorites/Count
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    count = 0
                });
            }

            var count = await _context.Favorites
                .CountAsync(f => f.UserId == userId);

            return Json(new
            {
                count
            });
        }
    }
}