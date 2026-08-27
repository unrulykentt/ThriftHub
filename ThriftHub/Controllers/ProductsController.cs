using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppStorageService _storage;
        private readonly SellerSubscriptionService _subscriptionService;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public ProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AppStorageService storage,
            SellerSubscriptionService subscriptionService)
        {
            _context = context;
            _userManager = userManager;
            _storage = storage;
            _subscriptionService = subscriptionService;
        }


        // ============================================================
        // GET CURRENT USER
        // ============================================================

        private async Task<ApplicationUser?> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(User);
        }


        // ============================================================
        // CHECK ADMIN
        // ============================================================

        private async Task<bool> IsAdmin(ApplicationUser user)
        {
            return string.Equals(
                       user.UserType,
                       "Admin",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   await _userManager.IsInRoleAsync(
                       user,
                       "Admin"
                   );
        }


        // ============================================================
        // CHECK SELLER ACCESS
        // ============================================================

        private async Task<bool> CanSellerManageProducts(
            ApplicationUser user)
        {
            // --------------------------------------------------------
            // ADMIN
            // --------------------------------------------------------

            if (await IsAdmin(user))
            {
                return true;
            }


            // --------------------------------------------------------
            // SUSPENDED ACCOUNT
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                return false;
            }


            // --------------------------------------------------------
            // MUST BE SELLER
            // --------------------------------------------------------

            if (!string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }


            // --------------------------------------------------------
            // SELLER VERIFICATION
            // --------------------------------------------------------

            if (!user.IsVerified ||
                !string.Equals(
                    user.VerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }


            // --------------------------------------------------------
            // ACTIVE SUBSCRIPTION
            // --------------------------------------------------------

            return await _subscriptionService
                .HasActiveSubscriptionAsync(
                    user.Id);
        }


        // ============================================================
        // PRODUCT IMAGE DIRECTORY
        // ============================================================

        private string GetProductImageDirectory()
        {
            return _storage.GetUploadsCategoryPath(
                "products");
        }


        // ============================================================
        // PRODUCT IMAGE SAVE
        // ============================================================

        private async Task<string?> SaveProductImage(
            IFormFile? image)
        {
            if (image == null ||
                image.Length == 0)
            {
                return null;
            }


            // --------------------------------------------------------
            // ALLOWED FILE TYPES
            // --------------------------------------------------------

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
                    image.FileName)
                    .ToLowerInvariant();


            if (!allowedExtensions.Contains(
                    extension))
            {
                return null;
            }


            // --------------------------------------------------------
            // MAXIMUM SIZE = 5 MB
            // --------------------------------------------------------

            const long maximumFileSize =
                5 * 1024 * 1024;


            if (image.Length >
                maximumFileSize)
            {
                return null;
            }


            // --------------------------------------------------------
            // CREATE DIRECTORY
            // --------------------------------------------------------

            var directory =
                GetProductImageDirectory();


            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            // --------------------------------------------------------
            // GENERATE SAFE FILE NAME
            // --------------------------------------------------------

            var fileName =
                Guid.NewGuid()
                    .ToString("N")
                + extension;


            var filePath =
                Path.Combine(
                    directory,
                    fileName
                );


            // --------------------------------------------------------
            // SAVE FILE
            // --------------------------------------------------------

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew
                );


            await image.CopyToAsync(stream);


            // --------------------------------------------------------
            // RETURN PUBLIC URL
            // --------------------------------------------------------

            return _storage.BuildUploadsWebPath(
                "products",
                fileName);
        }


        // ============================================================
        // DELETE PRODUCT IMAGE
        // ============================================================

        private void DeleteProductImage(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }


            // --------------------------------------------------------
            // ONLY DELETE PRODUCT UPLOADS
            // --------------------------------------------------------

            var filePath =
                _storage.MapWebPathToPhysicalPath(
                    imageUrl);

            if (
                string.IsNullOrWhiteSpace(filePath) ||
                !System.IO.File.Exists(filePath))
            {
                return;
            }


            try
            {
                System.IO.File.Delete(filePath);
            }
            catch
            {
            }
        }


        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // BLOCK SUSPENDED USERS
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your account has been suspended."
                        : $"Your account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    "AccessDenied",
                    "Account"
                );
            }


            var products =
                await _context.Products
                    .OrderByDescending(
                        p => p.CreatedAt
                    )
                    .ToListAsync();


            return View(products);
        }


        // ============================================================
        // DETAILS
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(
            int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (product == null)
            {
                return NotFound();
            }


            return View(product);
        }


        // ============================================================
        // CREATE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // CHECK SELLER ACCESS
            // --------------------------------------------------------

            if (!await CanSellerManageProducts(user))
            {
                if (user.IsSuspended)
                {
                    TempData["ErrorMessage"] =
                        string.IsNullOrWhiteSpace(
                            user.SuspensionReason)
                            ? "Your suspended account cannot post products."
                            : $"Your suspended account cannot post products. Reason: {user.SuspensionReason}";
                }
                else if (!string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] =
                        "Only sellers can post products.";
                }
                else if (!user.IsVerified)
                {
                    TempData["ErrorMessage"] =
                        "Your seller account must be verified before posting products.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "You need an active seller subscription before posting products.";
                }


                return RedirectToAction(
                    nameof(Index)
                );
            }


            return View();
        }


        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile? image)
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // CHECK SELLER ACCESS
            // --------------------------------------------------------

            if (!await CanSellerManageProducts(user))
            {
                TempData["ErrorMessage"] =
                    user.IsSuspended
                        ? "Your suspended account cannot post products."
                        : "You do not currently have permission to post products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // VALIDATE MODEL
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(product);
            }


            // --------------------------------------------------------
            // SAVE IMAGE
            // --------------------------------------------------------

            if (image != null &&
                image.Length > 0)
            {
                var imageUrl =
                    await SaveProductImage(
                        image
                    );


                if (imageUrl == null)
                {
                    ModelState.AddModelError(
                        "image",
                        "Invalid image. Please upload JPG, JPEG, PNG or WEBP up to 5 MB."
                    );

                    return View(product);
                }


                product.ImageUrl =
                    imageUrl;
            }


            // --------------------------------------------------------
            // ASSIGN SELLER
            // --------------------------------------------------------

            product.SellerId =
                user.Id;


            product.IsSold =
                false;


            product.CreatedAt =
                DateTime.UtcNow;


            // --------------------------------------------------------
            // SAVE PRODUCT
            // --------------------------------------------------------

            _context.Products.Add(
                product
            );


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your product has been posted successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // EDIT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // CHECK SUSPENSION
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "Your suspended account cannot edit products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (product == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // ADMIN CAN EDIT
            // --------------------------------------------------------

            if (await IsAdmin(user))
            {
                return View(product);
            }


            // --------------------------------------------------------
            // SELLER MUST OWN PRODUCT
            // --------------------------------------------------------

            if (product.SellerId != user.Id)
            {
                TempData["ErrorMessage"] =
                    "You can only edit your own products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // CHECK SELLER ACCESS
            // --------------------------------------------------------

            if (!await CanSellerManageProducts(user))
            {
                TempData["ErrorMessage"] =
                    "You do not currently have permission to edit products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            return View(product);
        }


        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product model,
            IFormFile? image)
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // SUSPENSION
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "Your suspended account cannot edit products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (product == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // ADMIN
            // --------------------------------------------------------

            var isAdmin =
                await IsAdmin(user);


            // --------------------------------------------------------
            // OWNER CHECK
            // --------------------------------------------------------

            if (!isAdmin &&
                product.SellerId != user.Id)
            {
                TempData["ErrorMessage"] =
                    "You can only edit your own products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // SELLER PERMISSION
            // --------------------------------------------------------

            if (!isAdmin &&
                !await CanSellerManageProducts(user))
            {
                TempData["ErrorMessage"] =
                    "You do not currently have permission to edit products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // MODEL VALIDATION
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                model.Id =
                    id;

                model.SellerId =
                    product.SellerId;

                model.ImageUrl =
                    product.ImageUrl;

                return View(model);
            }


            // --------------------------------------------------------
            // UPDATE PRODUCT
            // --------------------------------------------------------

            product.Name =
                model.Name;

            product.Description =
                model.Description;

            product.Price =
                model.Price;

            product.Category =
                model.Category;

            product.Subcategory =
                model.Subcategory;

            product.Condition =
                model.Condition;

            product.Sizes =
                model.Sizes;

            product.IsSold =
                model.IsSold;


            // --------------------------------------------------------
            // REPLACE IMAGE
            // --------------------------------------------------------

            if (image != null &&
                image.Length > 0)
            {
                var newImageUrl =
                    await SaveProductImage(
                        image
                    );


                if (newImageUrl == null)
                {
                    ModelState.AddModelError(
                        "image",
                        "Invalid image. Please upload JPG, JPEG, PNG or WEBP up to 5 MB."
                    );

                    model.ImageUrl =
                        product.ImageUrl;

                    return View(model);
                }


                var oldImageUrl =
                    product.ImageUrl;


                product.ImageUrl =
                    newImageUrl;


                DeleteProductImage(
                    oldImageUrl
                );
            }


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Product updated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // DELETE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // SUSPENDED USERS CANNOT DELETE
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "Your suspended account cannot delete products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // ADMIN
            // --------------------------------------------------------

            var isAdmin =
                await IsAdmin(user);


            // --------------------------------------------------------
            // OWNER
            // --------------------------------------------------------

            if (!isAdmin &&
                product.SellerId != user.Id)
            {
                TempData["ErrorMessage"] =
                    "You can only delete your own products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // SELLER PERMISSION
            // --------------------------------------------------------

            if (!isAdmin &&
                !await CanSellerManageProducts(user))
            {
                TempData["ErrorMessage"] =
                    "You do not currently have permission to delete products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // DELETE IMAGE
            // --------------------------------------------------------

            DeleteProductImage(
                product.ImageUrl
            );


            // --------------------------------------------------------
            // DELETE PRODUCT
            // --------------------------------------------------------

            _context.Products.Remove(
                product
            );


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Product deleted successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // MY PRODUCTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyProducts()
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // SUSPENDED ACCOUNT
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "Suspended accounts cannot manage products.";

                return RedirectToAction(
                    "AccessDenied",
                    "Account"
                );
            }


            var products =
                await _context.Products
                    .Where(
                        p => p.SellerId == user.Id
                    )
                    .OrderByDescending(
                        p => p.CreatedAt
                    )
                    .ToListAsync();


            return View(products);
        }


        // ============================================================
        // MARK PRODUCT AS SOLD
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(
            int id)
        {
            var user =
                await GetCurrentUser();


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // SUSPENSION
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "Your suspended account cannot update products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // ADMIN
            // --------------------------------------------------------

            var isAdmin =
                await IsAdmin(user);


            // --------------------------------------------------------
            // OWNER
            // --------------------------------------------------------

            if (!isAdmin &&
                product.SellerId != user.Id)
            {
                TempData["ErrorMessage"] =
                    "You can only update your own products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // SELLER PERMISSION
            // --------------------------------------------------------

            if (!isAdmin &&
                !await CanSellerManageProducts(user))
            {
                TempData["ErrorMessage"] =
                    "You do not currently have permission to update products.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            product.IsSold =
                true;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Product has been marked as sold.";


            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}