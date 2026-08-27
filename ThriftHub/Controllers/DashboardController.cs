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
    public class DashboardController : Controller
    {
        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AppStorageService _storage;
        private readonly SellerSubscriptionService _subscriptionService;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            AppStorageService storage,
            SellerSubscriptionService subscriptionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _storage = storage;
            _subscriptionService = subscriptionService;
        }


        // ============================================================
        // CHECK ADMIN
        // ============================================================

        private async Task<bool> IsAdmin(
            ApplicationUser user)
        {
            // --------------------------------------------------------
            // DATABASE USER TYPE
            // --------------------------------------------------------

            if (string.Equals(
                user.UserType,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            // --------------------------------------------------------
            // ASP.NET IDENTITY ROLE
            // --------------------------------------------------------

            return await _userManager.IsInRoleAsync(
                user,
                "Admin");
        }


        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // --------------------------------------------------------
            // GET CURRENT USER DIRECTLY FROM DATABASE
            // --------------------------------------------------------

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // ========================================================
            // CHECK ACCOUNT SUSPENSION
            // ========================================================

            if (user.IsSuspended)
            {
                user.IsOnline = false;

                await _userManager.UpdateAsync(user);

                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    "AccessDenied",
                    "Account");
            }


            // ========================================================
            // DETERMINE CURRENT ACCOUNT TYPE
            // ========================================================
            //
            // IMPORTANT:
            // The database UserType is checked first.
            //
            // This prevents an old login session from making an
            // administrator appear as a customer.
            //
            // ========================================================

            var isAdmin =
                await IsAdmin(user);


            var isSeller =
                string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase);


            // ========================================================
            // REFRESH ADMIN LOGIN SESSION
            // ========================================================
            //
            // If the database says this user is an Admin but the
            // current login session does not have the Admin role,
            // refresh the authentication session.
            //
            // This means the user should NOT have to manually
            // logout and login again after becoming an Admin.
            //
            // ========================================================

            if (isAdmin)
            {
                var hasAdminRole =
                    await _userManager.IsInRoleAsync(
                        user,
                        "Admin");


                // ----------------------------------------------------
                // MAKE SURE ADMIN ROLE EXISTS
                // ----------------------------------------------------

                if (!hasAdminRole)
                {
                    var roleResult =
                        await _userManager.AddToRoleAsync(
                            user,
                            "Admin");


                    if (roleResult.Succeeded)
                    {
                        // Refresh the login session after adding
                        // the Admin role.
                        await _signInManager.RefreshSignInAsync(
                            user);
                    }
                }
                else
                {
                    // ------------------------------------------------
                    // REFRESH EXISTING ADMIN SESSION
                    // ------------------------------------------------

                    await _signInManager.RefreshSignInAsync(
                        user);
                }
            }


            // ========================================================
            // USER INFORMATION
            // ========================================================

            ViewBag.FullName =
                user.FullName;

            ViewBag.Email =
                user.Email;


            // --------------------------------------------------------
            // IMPORTANT
            // --------------------------------------------------------
            //
            // Always use the database account type.
            //
            // --------------------------------------------------------

            if (isAdmin)
            {
                ViewBag.UserType =
                    "Admin";

                ViewBag.AccountType =
                    "Admin";
            }
            else if (isSeller)
            {
                ViewBag.UserType =
                    "Seller";

                ViewBag.AccountType =
                    "Seller";
            }
            else
            {
                ViewBag.UserType =
                    "Customer";

                ViewBag.AccountType =
                    "Customer";
            }


            ViewBag.IsAdmin =
                isAdmin;


            ViewBag.IsVerified =
                user.IsVerified;


            ViewBag.VerificationStatus =
                user.VerificationStatus;


            ViewBag.IsOnline =
                user.IsOnline;


            ViewBag.ProfileImageUrl =
                UserPresentationHelper.ResolveProfileImageUrl(
                    user.ProfileImageUrl)
                ?? string.Empty;

            ViewBag.ProfileInitials =
                UserPresentationHelper.GetInitials(
                    UserPresentationHelper.GetDisplayName(user));


            // ========================================================
            // SELLER INFORMATION
            // ========================================================

            ViewBag.IsSeller =
                isSeller;


            // ========================================================
            // ACTIVE SELLER SUBSCRIPTION
            // ========================================================
            //
            // Seller verification DOES NOT automatically give
            // permission to post products.
            //
            // An active subscription gives the seller permission
            // to post products.
            //
            // ========================================================

            bool hasActiveSubscription =
                false;

            SellerSubscription? activeSubscription =
                null;

            if (isSeller)
            {
                activeSubscription =
                    await _subscriptionService
                        .GetActiveSubscriptionAsync(
                            user.Id);

                hasActiveSubscription =
                    activeSubscription != null;
            }


            ViewBag.HasActiveSubscription =
                hasActiveSubscription;

            ViewBag.ActiveSubscription =
                activeSubscription;

            ViewBag.IsWelcomeTrialActive =
                activeSubscription != null
                && SellerSubscriptionService.IsWelcomeTrial(
                    activeSubscription);

            if (activeSubscription != null)
            {
                ViewBag.TrialDaysRemaining =
                    SellerSubscriptionService.GetDaysRemaining(
                        activeSubscription,
                        DateTime.UtcNow);
            }


            // ========================================================
            // ADMIN INFORMATION
            // ========================================================

            if (isAdmin)
            {
                ViewBag.AdminDashboard =
                    true;

                ViewBag.AdminName =
                    string.IsNullOrWhiteSpace(
                        user.FullName)
                        ? "ThriftHub Administrator"
                        : user.FullName;
            }
            else
            {
                ViewBag.AdminDashboard =
                    false;
            }


            // ========================================================
            // RETURN DASHBOARD
            // ========================================================

            return View();
        }


        // ============================================================
        // BECOME A SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeSeller()
        {
            // --------------------------------------------------------
            // GET CURRENT USER
            // --------------------------------------------------------

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // ========================================================
            // CHECK ACCOUNT SUSPENSION
            // ========================================================

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your suspended account cannot become a seller."
                        : $"Your suspended account cannot become a seller. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // CHECK ADMIN FIRST
            // ========================================================

            var isAdmin =
                await IsAdmin(user);


            if (isAdmin)
            {
                TempData["ErrorMessage"] =
                    "Administrator accounts cannot become seller accounts.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // ALREADY A SELLER
            // ========================================================

            if (string.Equals(
                user.UserType,
                "Seller",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Your account is already a seller account.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // DIRECT CUSTOMERS TO ID VERIFICATION FIRST
            // ========================================================

            TempData["SuccessMessage"] =
                "Please upload your government ID documents to apply as a seller.";

            return RedirectToAction(
                "SellerVerification",
                "Account");
        }


        // ============================================================
        // UPLOAD PROFILE PHOTO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(
            IFormFile? photo)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (photo == null ||
                photo.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please choose a profile photo.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (photo.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] =
                    "Profile photo must not exceed 5 MB.";

                return RedirectToAction(
                    nameof(Index));
            }

            var contentType =
                photo.ContentType ?? string.Empty;

            if (!contentType.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Please upload an image file (JPG, PNG, or WebP).";

                return RedirectToAction(
                    nameof(Index));
            }

            var extension =
                Path.GetExtension(photo.FileName)
                    .ToLowerInvariant();

            if (extension is not ".jpg"
                and not ".jpeg"
                and not ".png"
                and not ".webp"
                and not ".gif")
            {
                extension = ".jpg";
            }

            var uploadsFolder =
                _storage.GetUploadsCategoryPath(
                    "profiles");

            var uniqueFileName =
                $"{user.Id}-{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var previousPhotoPath =
                _storage.MapWebPathToPhysicalPath(
                    user.ProfileImageUrl);

            user.ProfileImageUrl =
                _storage.BuildUploadsWebPath(
                    "profiles",
                    uniqueFileName);

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                System.IO.File.Delete(filePath);

                TempData["ErrorMessage"] =
                    "Profile photo could not be saved. Please try again.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(previousPhotoPath) &&
                System.IO.File.Exists(previousPhotoPath))
            {
                try
                {
                    System.IO.File.Delete(previousPhotoPath);
                }
                catch
                {
                }
            }

            TempData["SuccessMessage"] =
                "Profile photo updated successfully.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}