using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    public class MarketPlaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ProductViewService _productViewService;
        private readonly ProductImageService _productImageService;

        public MarketPlaceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ProductViewService productViewService,
            ProductImageService productImageService)
        {
            _context = context;
            _userManager = userManager;
            _productViewService = productViewService;
            _productImageService = productImageService;
        }


        // =========================================================
        // MARKETPLACE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? category,
            string? subcategory,
            decimal? minPrice,
            decimal? maxPrice)
        {
            IQueryable<Product> products =
                _context.Products.AsNoTracking();


            // =====================================================
            // CLEAN INPUTS
            // =====================================================

            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            category = string.IsNullOrWhiteSpace(category)
                ? null
                : category.Trim();

            subcategory = string.IsNullOrWhiteSpace(subcategory)
                ? null
                : subcategory.Trim();


            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchText = search.ToLower();

                products = products.Where(p =>
                    (p.Name != null &&
                     p.Name.ToLower().Contains(searchText))

                    ||

                    (p.Description != null &&
                     p.Description.ToLower().Contains(searchText))

                    ||

                    (p.Category != null &&
                     p.Category.ToLower().Contains(searchText))

                    ||

                    (p.Subcategory != null &&
                     p.Subcategory.ToLower().Contains(searchText))

                    ||

                    (p.Condition != null &&
                     p.Condition.ToLower().Contains(searchText))
                );
            }


            // =====================================================
            // CATEGORY
            // =====================================================

            if (!string.IsNullOrWhiteSpace(category) &&
                !category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                var matchValues =
                    MarketplaceCategoryCatalog.GetMatchValues(category);

                products = products.Where(p =>
                    p.Category != null &&
                    matchValues.Contains(
                        p.Category.ToLower()));
            }


            // =====================================================
            // SUBCATEGORY
            // =====================================================

            if (!string.IsNullOrWhiteSpace(subcategory) &&
                !subcategory.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                var selectedSubcategory =
                    subcategory.ToLower();

                products = products.Where(p =>
                    p.Subcategory != null &&
                    p.Subcategory.ToLower() ==
                    selectedSubcategory);
            }


            // =====================================================
            // MINIMUM PRICE
            // =====================================================

            if (minPrice.HasValue &&
                minPrice.Value >= 0)
            {
                products = products.Where(p =>
                    p.Price >= minPrice.Value);
            }


            // =====================================================
            // MAXIMUM PRICE
            // =====================================================

            if (maxPrice.HasValue &&
                maxPrice.Value >= 0)
            {
                products = products.Where(p =>
                    p.Price <= maxPrice.Value);
            }


            // =====================================================
            // ONLY AVAILABLE PRODUCTS
            // =====================================================

            products = products.Where(p =>
                !p.IsSold);


            // =====================================================
            // GET PRODUCTS
            // =====================================================

            var result =
                await products
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();


            // =====================================================
            // USER FAVORITES
            // =====================================================

            var userId =
                _userManager.GetUserId(User);

            var favoriteProductIds =
                new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var favoriteIds =
                    await _context.Favorites
                        .AsNoTracking()
                        .Where(f =>
                            f.UserId == userId)
                        .Select(f =>
                            f.ProductId)
                        .ToListAsync();

                favoriteProductIds =
                    favoriteIds.ToHashSet();
            }

            ViewBag.FavoriteProductIds =
                favoriteProductIds;

            ViewBag.SellerDisplayNames =
                await UserPresentationHelper.LoadSellerDisplayNamesAsync(
                    _context,
                    result);


            // =====================================================
            // SEND FILTER VALUES TO VIEW
            // =====================================================

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Subcategory = subcategory;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;


            // =====================================================
            // CATEGORIES
            // =====================================================

            ViewBag.Categories =
                MarketplaceCategoryCatalog.CategoryNames;

            ViewBag.MarketplaceCategories =
                MarketplaceCategoryCatalog.All;


            // =====================================================
            // SUBCATEGORIES
            // =====================================================

            ViewBag.Subcategories =
                MarketplaceCategoryCatalog.GetSubcategories(category);


            return View(result);
        }


        // =========================================================
        // ADD / REMOVE FAVORITE
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(
            int productId,
            string? returnUrl)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            // =====================================================
            // CHECK PRODUCT
            // =====================================================

            var product =
                await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == productId);


            if (product == null)
            {
                return NotFound();
            }


            // =====================================================
            // CHECK EXISTING FAVORITE
            // =====================================================

            var existingFavorite =
                await _context.Favorites
                    .FirstOrDefaultAsync(f =>
                        f.UserId == userId &&
                        f.ProductId == productId);


            // =====================================================
            // REMOVE
            // =====================================================

            if (existingFavorite != null)
            {
                _context.Favorites.Remove(
                    existingFavorite);
            }

            // =====================================================
            // ADD
            // =====================================================

            else
            {
                var favorite =
                    new Favorite
                    {
                        UserId = userId,
                        ProductId = productId,
                        CreatedAt = DateTime.UtcNow
                    };

                _context.Favorites.Add(favorite);
            }


            await _context.SaveChangesAsync();


            // =====================================================
            // RETURN TO PREVIOUS PAGE
            // =====================================================

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // WISHLIST
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Wishlist()
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            var favorites =
                await _context.Favorites
                    .AsNoTracking()
                    .Where(f =>
                        f.UserId == userId)
                    .OrderByDescending(
                        f => f.CreatedAt)
                    .ToListAsync();


            var productIds =
                favorites
                    .Select(f => f.ProductId)
                    .ToList();


            var products =
                await _context.Products
                    .AsNoTracking()
                    .Where(p =>
                        productIds.Contains(p.Id))
                    .ToListAsync();


            ViewBag.FavoriteProductIds =
                productIds.ToHashSet();


            return View(products);
        }


        // =========================================================
        // PRODUCT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id.Value);


            if (product == null)
            {
                return NotFound();
            }


            await _productViewService.RecordViewAsync(
                product,
                HttpContext,
                _userManager.GetUserId(User));


            ApplicationUser? seller = null;


            if (!string.IsNullOrWhiteSpace(
                    product.SellerId))
            {
                seller =
                    await _userManager
                        .FindByIdAsync(
                            product.SellerId);
            }


            ViewBag.Seller = seller;

            var productImages =
                await _productImageService
                    .GetProductImageUrlsAsync(
                        product.Id,
                        product.ImageUrl);

            ViewBag.ProductImages = productImages;

            var reviews =
                await _context.ProductReviews
                    .AsNoTracking()
                    .Where(review =>
                        review.ProductId == product.Id)
                    .OrderByDescending(
                        review => review.CreatedAt)
                    .ToListAsync();

            var reviewerIds =
                reviews
                    .Select(review => review.UserId)
                    .Distinct()
                    .ToList();

            var reviewers =
                reviewerIds.Count == 0
                    ? []
                    : await _context.Users
                        .AsNoTracking()
                        .Where(user =>
                            reviewerIds.Contains(user.Id))
                        .ToListAsync();

            var reviewerNames =
                reviewers.ToDictionary(
                    user => user.Id,
                    user =>
                        UserPresentationHelper.GetDisplayName(
                            user));

            ViewBag.Reviews = reviews;
            ViewBag.ReviewerNames = reviewerNames;
            ViewBag.AverageRating =
                reviews.Count == 0
                    ? 0.0
                    : reviews.Average(
                        review => review.Rating);
            ViewBag.ReviewCount = reviews.Count;

            var currentUserId =
                _userManager.GetUserId(User);

            ProductReview? userReview = null;

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                userReview =
                    reviews.FirstOrDefault(review =>
                        review.UserId == currentUserId);
            }

            ViewBag.UserReview = userReview;
            ViewBag.CanReview =
                User.Identity?.IsAuthenticated == true &&
                !string.IsNullOrWhiteSpace(currentUserId) &&
                product.SellerId != currentUserId;


            return View(product);
        }


        // =========================================================
        // MARK PRODUCT AS SOLD
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(
            int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            var currentUserId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                    currentUserId) ||
                product.SellerId != currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You are not allowed to manage this product.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = product.Id
                    });
            }


            product.IsSold = true;

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your product has been marked as sold.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = product.Id
                });
        }


        // =========================================================
        // MARK PRODUCT AS AVAILABLE
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsAvailable(
            int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            var currentUserId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                    currentUserId) ||
                product.SellerId != currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You are not allowed to manage this product.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = product.Id
                    });
            }


            product.IsSold = false;

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your product is now available again.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = product.Id
                });
        }
    }
}