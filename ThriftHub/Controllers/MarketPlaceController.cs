using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    public class MarketPlaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MarketPlaceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                var selectedCategory =
                    category.ToLower();

                products = products.Where(p =>
                    p.Category != null &&
                    p.Category.ToLower() ==
                    selectedCategory);
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

            ViewBag.Categories = new List<string>
            {
                "Women's Fashion",
                "Men's Fashion",
                "Kids",
                "Shoes",
                "Bags",
                "Accessories",

                "Books & Textbooks",
                "Laptops & Computers",
                "Phones & Tablets",
                "Electronics",
                "Computer Accessories",
                "Chargers & Cables",
                "Power Banks",
                "Stationery & School Supplies",
                "Calculators",
                "Backpacks & School Bags",
                "Hostel Essentials"
            };


            // =====================================================
            // SUBCATEGORIES
            // =====================================================

            ViewBag.Subcategories =
                GetSubcategories(category);


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
        // GET SUBCATEGORIES
        // =========================================================

        private List<string> GetSubcategories(
            string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return new List<string>();
            }


            return category.Trim().ToLower() switch
            {
                "women" =>
                    new List<string>
                    {
                        "Dresses",
                        "Tops",
                        "Skirts",
                        "Trousers",
                        "Jeans",
                        "Jackets",
                        "Shorts",
                        "Jumpsuits",
                        "Traditional Wear"
                    },

                "women's fashion" =>
                    new List<string>
                    {
                        "Dresses",
                        "Tops",
                        "Skirts",
                        "Trousers",
                        "Jeans",
                        "Jackets",
                        "Shorts",
                        "Jumpsuits",
                        "Traditional Wear"
                    },


                "men" =>
                    new List<string>
                    {
                        "T-Shirts",
                        "Shirts",
                        "Trousers",
                        "Jeans",
                        "Shorts",
                        "Jackets",
                        "Suits",
                        "Traditional Wear"
                    },

                "men's fashion" =>
                    new List<string>
                    {
                        "T-Shirts",
                        "Shirts",
                        "Trousers",
                        "Jeans",
                        "Shorts",
                        "Jackets",
                        "Suits",
                        "Traditional Wear"
                    },


                "kids" =>
                    new List<string>
                    {
                        "Boys Clothing",
                        "Girls Clothing",
                        "Baby Clothing",
                        "Kids Shoes",
                        "Kids Accessories"
                    },


                "shoes" =>
                    new List<string>
                    {
                        "Sneakers",
                        "Slippers",
                        "Sandals",
                        "Heels",
                        "Boots",
                        "Formal Shoes",
                        "Sports Shoes"
                    },


                "bags" =>
                    new List<string>
                    {
                        "Handbags",
                        "Backpacks",
                        "Travel Bags",
                        "School Bags",
                        "Laptop Bags",
                        "Crossbody Bags",
                        "Wallets"
                    },


                "accessories" =>
                    new List<string>
                    {
                        "Watches",
                        "Belts",
                        "Caps",
                        "Hats",
                        "Sunglasses",
                        "Jewelry",
                        "Scarves"
                    },


                "books & textbooks" =>
                    new List<string>
                    {
                        "Textbooks",
                        "Novels",
                        "Course Materials",
                        "Lecture Notes",
                        "Past Questions",
                        "Research Materials",
                        "Dictionaries",
                        "E-Books"
                    },


                "laptops & computers" =>
                    new List<string>
                    {
                        "Laptops",
                        "Desktop Computers",
                        "Monitors",
                        "MacBooks",
                        "Chromebooks",
                        "Mini PCs",
                        "Computer Sets"
                    },


                "phones & tablets" =>
                    new List<string>
                    {
                        "Smartphones",
                        "iPhones",
                        "Android Phones",
                        "Tablets",
                        "iPads",
                        "Feature Phones"
                    },


                "electronics" =>
                    new List<string>
                    {
                        "Televisions",
                        "Speakers",
                        "Headphones",
                        "Earphones",
                        "Cameras",
                        "Game Consoles",
                        "Projectors",
                        "Smart Watches"
                    },


                "computer accessories" =>
                    new List<string>
                    {
                        "Keyboards",
                        "Mice",
                        "Mouse Pads",
                        "Flash Drives",
                        "External Hard Drives",
                        "Memory Cards",
                        "Webcams",
                        "Laptop Stands",
                        "Cooling Pads"
                    },


                "chargers & cables" =>
                    new List<string>
                    {
                        "Phone Chargers",
                        "Laptop Chargers",
                        "USB Cables",
                        "Lightning Cables",
                        "HDMI Cables",
                        "Extension Cables",
                        "Adapters",
                        "Charging Stations"
                    },


                "power banks" =>
                    new List<string>
                    {
                        "10,000mAh",
                        "20,000mAh",
                        "30,000mAh",
                        "Wireless Power Banks",
                        "Solar Power Banks"
                    },


                "stationery & school supplies" =>
                    new List<string>
                    {
                        "Notebooks",
                        "Exercise Books",
                        "Pens",
                        "Pencils",
                        "Markers",
                        "Files & Folders",
                        "Rulers",
                        "Geometry Sets",
                        "Sticky Notes",
                        "Art Supplies"
                    },


                "calculators" =>
                    new List<string>
                    {
                        "Scientific Calculators",
                        "Financial Calculators",
                        "Basic Calculators",
                        "Graphing Calculators",
                        "Engineering Calculators"
                    },


                "backpacks & school bags" =>
                    new List<string>
                    {
                        "Backpacks",
                        "Laptop Backpacks",
                        "School Bags",
                        "Laptop Bags",
                        "Messenger Bags",
                        "Travel Backpacks"
                    },


                "hostel essentials" =>
                    new List<string>
                    {
                        "Beds & Mattresses",
                        "Pillows",
                        "Bedsheets",
                        "Blankets",
                        "Fans",
                        "Study Tables",
                        "Chairs",
                        "Wardrobes",
                        "Kitchen Items",
                        "Cooking Equipment",
                        "Storage Items",
                        "Cleaning Supplies",
                        "Iron & Ironing Boards",
                        "Mosquito Nets"
                    },


                _ => new List<string>()
            };
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
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == id.Value);


            if (product == null)
            {
                return NotFound();
            }


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