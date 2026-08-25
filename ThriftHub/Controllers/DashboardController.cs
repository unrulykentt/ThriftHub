using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

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


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
                user.ProfileImageUrl;


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


            if (isSeller)
            {
                var now =
                    DateTime.UtcNow;


                var subscription =
                    await _context.SellerSubscriptions
                        .Where(s =>
                            s.SellerId == user.Id &&
                            s.Status == "Active" &&
                            s.EndDate > now
                        )
                        .OrderByDescending(
                            s => s.EndDate
                        )
                        .FirstOrDefaultAsync();


                if (subscription != null)
                {
                    // ------------------------------------------------
                    // ADMIN-GRANTED SUBSCRIPTION
                    // ------------------------------------------------

                    if (subscription.IsAdminGranted)
                    {
                        hasActiveSubscription =
                            true;
                    }

                    // ------------------------------------------------
                    // NORMAL PAID SUBSCRIPTION
                    // ------------------------------------------------

                    else if (string.Equals(
                        subscription.PaymentStatus,
                        "Paid",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        hasActiveSubscription =
                            true;
                    }
                }
            }


            ViewBag.HasActiveSubscription =
                hasActiveSubscription;


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
            // CHANGE CUSTOMER TO SELLER
            // ========================================================

            user.UserType =
                "Seller";


            // ========================================================
            // SELLER VERIFICATION
            // ========================================================
            //
            // Seller verification is still required.
            //
            // Verification does NOT automatically give permission
            // to post products.
            //
            // Subscription controls posting permission.
            //
            // ========================================================

            user.IsVerified =
                false;

            user.VerificationStatus =
                "Pending";


            // ========================================================
            // SAVE USER
            // ========================================================

            var result =
                await _userManager.UpdateAsync(
                    user);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                TempData["ErrorMessage"] =
                    "We could not process your seller request.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ========================================================
            // REFRESH LOGIN SESSION
            // ========================================================
            //
            // The account has changed from Customer to Seller.
            // Refresh the authentication cookie immediately.
            //
            // ========================================================

            await _signInManager.RefreshSignInAsync(
                user);


            // ========================================================
            // SUCCESS
            // ========================================================

            TempData["SuccessMessage"] =
                "Your seller request has been submitted. Please wait for verification.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}