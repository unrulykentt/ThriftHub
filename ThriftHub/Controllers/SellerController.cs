using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SellerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ============================================================
        // SELLER HOME
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ADMINISTRATOR
            // --------------------------------------------------------

            if (string.Equals(
                user.UserType,
                "Administrator",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Admin");
            }


            // --------------------------------------------------------
            // ONLY SELLERS
            // --------------------------------------------------------

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "You need a seller account to access this page.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            return View();
        }


        // ============================================================
        // CREATE PRODUCT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // ========================================================
            // ONLY SELLERS
            // ========================================================

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "You must become a seller before posting an item.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // ========================================================
            // ACTIVE SUBSCRIPTION REQUIRED
            // ========================================================

            var hasActiveSubscription =
                await HasActiveSubscription(user.Id);

            if (!hasActiveSubscription)
            {
                TempData["ErrorMessage"] =
                    "You need an active seller subscription before you can post an item.";

                return RedirectToAction(
                    "Index",
                    "Subscription");
            }


            return View();
        }


        // ============================================================
        // CREATE PRODUCT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product model,
            IFormFile? productImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // ========================================================
            // ONLY SELLERS
            // ========================================================

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only seller accounts can post items for sale.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // ========================================================
            // ACTIVE SUBSCRIPTION REQUIRED
            // ========================================================

            var hasActiveSubscription =
                await HasActiveSubscription(user.Id);

            if (!hasActiveSubscription)
            {
                TempData["ErrorMessage"] =
                    "You need an active seller subscription before you can post an item.";

                return RedirectToAction(
                    "Index",
                    "Subscription");
            }


            // ========================================================
            // SYSTEM CONTROLLED FIELDS
            // ========================================================

            ModelState.Remove("Id");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("SellerId");
            ModelState.Remove("CreatedAt");


            // ========================================================
            // VALIDATE PRODUCT
            // ========================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ========================================================
            // PRODUCT IMAGE
            // ========================================================

            string? imageUrl = null;


            if (productImage != null &&
                productImage.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                var extension =
                    Path.GetExtension(
                        productImage.FileName)
                        .ToLowerInvariant();


                if (!allowedExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        "productImage",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    return View(model);
                }


                const long maxFileSize =
                    10 * 1024 * 1024;


                if (productImage.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        "productImage",
                        "The product image must be smaller than 10 MB.");

                    return View(model);
                }


                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "products");


                if (!Directory.Exists(
                    uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }


                var fileName =
                    Guid.NewGuid()
                        .ToString("N")
                    + extension;


                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);


                using (
                    var stream =
                        new FileStream(
                            filePath,
                            FileMode.Create))
                {
                    await productImage.CopyToAsync(
                        stream);
                }


                imageUrl =
                    "/uploads/products/"
                    + fileName;
            }


            // ========================================================
            // CREATE PRODUCT
            // ========================================================

            var product =
                new Product
                {
                    Name =
                        model.Name,

                    Description =
                        model.Description,

                    Price =
                        model.Price,

                    Category =
                        model.Category,

                    Subcategory =
                        model.Subcategory,

                    Condition =
                        model.Condition,

                    Sizes =
                        model.Sizes,

                    ImageUrl =
                        imageUrl,

                    IsSold =
                        false,

                    SellerId =
                        user.Id,

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.Products.Add(product);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your item has been posted successfully.";


            return RedirectToAction(
                nameof(MyProducts));
        }


        // ============================================================
        // CHECK ACTIVE SUBSCRIPTION
        // ============================================================

        private async Task<bool> HasActiveSubscription(
            string sellerId)
        {
            var now =
                DateTime.UtcNow;


            var subscriptions =
                await _context.Set<SellerSubscription>()
                    .Where(s =>
                        s.SellerId == sellerId &&
                        s.EndDate > now)
                    .OrderByDescending(
                        s => s.EndDate)
                    .ToListAsync();


            foreach (var subscription in subscriptions)
            {
                if (!string.Equals(
                    subscription.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                // ----------------------------------------------------
                // ADMIN-GRANTED SUBSCRIPTION
                // ----------------------------------------------------

                if (subscription.IsAdminGranted)
                {
                    return true;
                }


                // ----------------------------------------------------
                // NORMAL PAID SUBSCRIPTION
                // ----------------------------------------------------

                if (string.Equals(
                    subscription.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            return false;
        }


        // ============================================================
        // MY PRODUCTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyProducts()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ONLY SELLERS
            // --------------------------------------------------------

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only sellers can view their products.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            var products =
                await _context.Products
                    .Where(p =>
                        p.SellerId != null &&
                        p.SellerId == user.Id)
                    .OrderByDescending(
                        p => p.Id)
                    .ToListAsync();


            return View(products);
        }


        // ============================================================
        // EDIT SOLD PRODUCT - GET
        // ============================================================
        //
        // This opens the edit page before a sold product is made
        // available again.
        //
        // The seller can change:
        // - Price
        // - Sizes
        // - Description
        // - Subcategory
        // - Condition
        //
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EditAvailable(int id)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ONLY SELLERS
            // --------------------------------------------------------

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only sellers can edit their products.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // --------------------------------------------------------
            // FIND PRODUCT BELONGING TO CURRENT SELLER
            // --------------------------------------------------------

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.SellerId == user.Id);


            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product not found or you do not own this product.";

                return RedirectToAction(
                    nameof(MyProducts));
            }


            // --------------------------------------------------------
            // PRODUCT MUST BE SOLD
            // --------------------------------------------------------

            if (!product.IsSold)
            {
                TempData["ErrorMessage"] =
                    "This product is already available.";

                return RedirectToAction(
                    nameof(MyProducts));
            }


            return View(product);
        }


        // ============================================================
        // EDIT SOLD PRODUCT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAvailable(
            Product model)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ONLY SELLERS
            // --------------------------------------------------------

            if (!string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only sellers can edit their products.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // --------------------------------------------------------
            // FIND ORIGINAL PRODUCT
            // --------------------------------------------------------

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == model.Id &&
                        p.SellerId == user.Id);


            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product not found or you do not own this product.";

                return RedirectToAction(
                    nameof(MyProducts));
            }


            // --------------------------------------------------------
            // PRODUCT MUST STILL BE SOLD
            // --------------------------------------------------------

            if (!product.IsSold)
            {
                TempData["ErrorMessage"] =
                    "This product is already available.";

                return RedirectToAction(
                    nameof(MyProducts));
            }


            // ========================================================
            // PRICE VALIDATION
            // ========================================================

            if (model.Price <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Price),
                    "Price must be greater than zero.");
            }


            // ========================================================
            // SIZE VALIDATION
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                model.Sizes))
            {
                ModelState.AddModelError(
                    nameof(model.Sizes),
                    "Please enter at least one available size.");
            }


            // ========================================================
            // INVALID FORM
            // ========================================================

            if (!ModelState.IsValid)
            {
                // Keep original image.
                model.ImageUrl =
                    product.ImageUrl;

                return View(model);
            }


            // ========================================================
            // UPDATE ALLOWED INFORMATION
            // ========================================================

            product.Price =
                model.Price;


            product.Sizes =
                model.Sizes?.Trim();


            product.Description =
                model.Description;


            product.Subcategory =
                model.Subcategory;


            product.Condition =
                model.Condition;


            // ========================================================
            // MAKE PRODUCT AVAILABLE
            // ========================================================

            product.IsSold =
                false;


            // ========================================================
            // SAVE
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // SUCCESS
            // ========================================================

            TempData["SuccessMessage"] =
                "Your product has been updated and is now available again.";


            return RedirectToAction(
                nameof(MyProducts));
        }


        // ============================================================
        // DELETE PRODUCT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // FIND ONLY CURRENT SELLER'S PRODUCT
            // --------------------------------------------------------

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id == id &&
                            p.SellerId == user.Id);


            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "You can only delete products that you posted.";

                return RedirectToAction(
                    nameof(MyProducts));
            }


            // ========================================================
            // DELETE IMAGE
            // ========================================================

            if (!string.IsNullOrWhiteSpace(
                product.ImageUrl))
            {
                var relativeImagePath =
                    product.ImageUrl
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar);


                var imagePath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        relativeImagePath);


                if (System.IO.File.Exists(
                    imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(
                            imagePath);
                    }
                    catch
                    {
                        // Continue deleting database record.
                    }
                }
            }


            // ========================================================
            // DELETE PRODUCT
            // ========================================================

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your product has been deleted successfully.";


            return RedirectToAction(
                nameof(MyProducts));
        }


        // ============================================================
        // SELLER PROFILE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Profile(
            string? id)
        {
            // --------------------------------------------------------
            // CHECK SELLER ID
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // FIND SELLER
            // --------------------------------------------------------

            var seller =
                await _userManager.FindByIdAsync(id);


            if (seller == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // GET AVAILABLE PRODUCTS
            // --------------------------------------------------------

            var products =
                await _context.Products
                    .AsNoTracking()
                    .Where(p =>
                        p.SellerId == seller.Id &&
                        !p.IsSold)
                    .OrderByDescending(
                        p => p.CreatedAt)
                    .ToListAsync();


            // --------------------------------------------------------
            // CREATE VIEW MODEL
            // --------------------------------------------------------

            var viewModel =
                new ThriftHub.ViewModels.SellerProfileViewModel
                {
                    Seller = seller,

                    Products = products,

                    TotalProducts = products.Count
                };


            return View(viewModel);
        }
    }
}