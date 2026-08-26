using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        private const string AdminEmail =
            "antwiagyeibright9@gmail.com";


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }


        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdminEmail =
                string.Equals(
                    user.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

            var isAdminRole =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");

            if (!isAdminEmail && !isAdminRole)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Admin"));

                if (!roleResult.Succeeded)
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard");
                }
            }

            if (!isAdminRole)
            {
                var addRoleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "Admin");

                if (!addRoleResult.Succeeded)
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard");
                }
            }

            var changed = false;

            if (user.UserType != "Admin")
            {
                user.UserType = "Admin";
                changed = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (!user.IsVerified)
            {
                user.IsVerified = true;
                changed = true;
            }

            if (!string.Equals(
                    user.VerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                user.VerificationStatus = "Approved";
                changed = true;
            }

            if (changed)
            {
                await _userManager.UpdateAsync(user);
            }


            // ========================================================
            // STATISTICS
            // ========================================================

            var totalUsers =
                await _userManager.Users.CountAsync();

            var totalSellers =
                await _userManager.Users
                    .CountAsync(u =>
                        u.UserType == "Seller");

            var totalCustomers =
                await _userManager.Users
                    .CountAsync(u =>
                        u.UserType == "Customer");

            var totalAdministrators =
                await _userManager.Users
                    .CountAsync(u =>
                        u.UserType == "Admin");

            var pendingVerification =
                await _userManager.Users
                    .CountAsync(u =>
                        u.VerificationStatus == "Pending" ||
                        u.VerificationStatus == "Verification Pending");

            var pendingIdentityVerification =
                await _userManager.Users
                    .CountAsync(u =>
                        !u.IdCardVerified &&
                        !string.IsNullOrWhiteSpace(u.IdCardType) &&
                        !string.IsNullOrWhiteSpace(u.IdCardNumber) &&
                        !string.IsNullOrWhiteSpace(u.IdCardFrontUrl));

            var verifiedSellers =
                await _userManager.Users
                    .CountAsync(u =>
                        u.UserType == "Seller" &&
                        u.IsVerified &&
                        u.VerificationStatus == "Approved");

            var totalProducts =
                await _context.Products.CountAsync();


            ViewBag.TotalUsers =
                totalUsers;

            ViewBag.TotalSellers =
                totalSellers;

            ViewBag.TotalCustomers =
                totalCustomers;

            ViewBag.TotalAdministrators =
                totalAdministrators;

            ViewBag.PendingVerification =
                pendingVerification;

            ViewBag.PendingIdentityVerification =
                pendingIdentityVerification;

            ViewBag.VerifiedSellers =
                verifiedSellers;

            ViewBag.TotalProducts =
                totalProducts;


            return View();
        }


        // ============================================================
        // MAKE CUSTOMER - SHOW EXISTING USERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MakeCustomer()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var users =
                await _userManager.Users
                    .Where(u =>
                        u.Id != currentUser.Id)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(users);
        }


        // ============================================================
        // MAKE CUSTOMER - CONVERT EXISTING USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCustomer(
            string id,
            string? returnTo = null)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user account.";

                return RedirectToAction(
                    nameof(MakeCustomer));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "The selected user could not be found.";

                return RedirectToAction(
                    nameof(MakeCustomer));
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");

            var isAdminEmail =
                string.Equals(
                    user.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

            if (isAdmin || isAdminEmail)
            {
                TempData["ErrorMessage"] =
                    "Administrator accounts cannot be converted into customers.";

                return RedirectToAction(
                    nameof(MakeCustomer));
            }

            if (string.Equals(
                    user.UserType,
                    "Customer",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    $"{user.FullName} is already a Customer.";

                return RedirectToAction(
                    nameof(MakeCustomer));
            }

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Seller"))
            {
                var removeSellerRole =
                    await _userManager.RemoveFromRoleAsync(
                        user,
                        "Seller");

                if (!removeSellerRole.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "The Seller role could not be removed.";

                    return RedirectToAction(
                        nameof(MakeCustomer));
                }
            }

            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                var createCustomerRole =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Customer"));

                if (!createCustomerRole.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "The Customer role could not be created.";

                    return RedirectToAction(
                        nameof(MakeCustomer));
                }
            }

            if (!await _userManager.IsInRoleAsync(
                    user,
                    "Customer"))
            {
                var addCustomerRole =
                    await _userManager.AddToRoleAsync(
                        user,
                        "Customer");

                if (!addCustomerRole.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "The Customer role could not be assigned.";

                    return RedirectToAction(
                        nameof(MakeCustomer));
                }
            }

            user.UserType =
                "Customer";

            user.IsVerified =
                false;

            user.VerificationStatus =
                "Pending";

            user.SubscriptionWaived =
                false;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "The account could not be converted to Customer.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }

                return RedirectToAction(
                    nameof(MakeCustomer));
            }

            TempData["SuccessMessage"] =
                $"{user.FullName} has been converted to a Customer successfully.";

            if (string.Equals(
                    returnTo,
                    "Users",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    nameof(Users));
            }

            return RedirectToAction(
                nameof(MakeCustomer));
        }


        // ============================================================
        // MANAGE USERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var users =
                await _userManager.Users
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(users);
        }


        // ============================================================
        // MANAGE SELLERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Sellers()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var sellers =
                await _userManager.Users
                    .Where(u =>
                        u.UserType == "Seller")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(sellers);
        }


        // ============================================================
        // SELLER VERIFICATION
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> SellerVerification()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var pendingSellers =
                await _userManager.Users
                    .Where(u =>
                        u.VerificationStatus == "Pending" ||
                        u.VerificationStatus == "Verification Pending")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(pendingSellers);
        }


        // ============================================================
        // APPROVE SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(
            string id,
            bool waiveSubscription = false)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid seller account.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }

            user.UserType =
                "Seller";

            user.IsVerified =
                true;

            user.VerificationStatus =
                "Approved";

            user.SubscriptionWaived =
                waiveSubscription;

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                if (waiveSubscription)
                {
                    TempData["SuccessMessage"] =
                        "Seller has been approved successfully with free subscription access.";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        "Seller has been approved successfully. A paid subscription is required before selling.";
                }
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Seller approval failed.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(SellerVerification));
        }


        // ============================================================
        // REJECT SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSeller(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid seller account.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }

            user.UserType =
                "Customer";

            user.IsVerified =
                false;

            user.VerificationStatus =
                "Rejected";

            user.SubscriptionWaived =
                false;

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Seller application has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to reject seller application.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(SellerVerification));
        }


        // ============================================================
        // ALL PRODUCTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var products =
                await _context.Products
                    .ToListAsync();

            return View(products);
        }


        // ============================================================
        // ADMINISTRATORS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Administrators()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var administrators =
                await _userManager.Users
                    .Where(u =>
                        u.UserType == "Admin")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(administrators);
        }


        // ============================================================
        // VERIFIED SELLERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VerifiedSellers()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var sellers =
                await _userManager.Users
                    .Where(u =>
                        u.UserType == "Seller" &&
                        u.IsVerified &&
                        u.VerificationStatus == "Approved")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(sellers);
        }


        // ============================================================
        // IDENTITY VERIFICATION
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> IdentityVerification()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var users =
                await _userManager.Users
                    .Where(u =>
                        !u.IdCardVerified &&
                        !string.IsNullOrWhiteSpace(u.IdCardType) &&
                        !string.IsNullOrWhiteSpace(u.IdCardNumber) &&
                        !string.IsNullOrWhiteSpace(u.IdCardFrontUrl))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(users);
        }


        // ============================================================
        // VERIFY IDENTITY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyIdentity(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user account.";

                return RedirectToAction(
                    nameof(IdentityVerification));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(IdentityVerification));
            }

            if (string.IsNullOrWhiteSpace(
                    user.IdCardFrontUrl))
            {
                TempData["ErrorMessage"] =
                    "This user has not submitted an ID document.";

                return RedirectToAction(
                    nameof(IdentityVerification));
            }

            user.IdCardVerified =
                true;

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Identity has been verified successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Identity verification failed.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(IdentityVerification));
        }


        // ============================================================
        // REJECT IDENTITY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectIdentity(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user account.";

                return RedirectToAction(
                    nameof(IdentityVerification));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(IdentityVerification));
            }

            user.IdCardVerified =
                false;

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Identity verification has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to reject identity verification.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(IdentityVerification));
        }


        // ============================================================
        // SUSPEND USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(
            string id,
            string? reason)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user account.";

                return RedirectToAction(
                    nameof(Users));
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser != null &&
                currentUser.Id == id)
            {
                TempData["ErrorMessage"] =
                    "You cannot suspend your own administrator account.";

                return RedirectToAction(
                    nameof(Users));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // PREVENT SUSPENDING ADMINISTRATORS
            // --------------------------------------------------------

            var targetIsAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");

            var targetIsAdminEmail =
                string.Equals(
                    user.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

            if (targetIsAdmin ||
                targetIsAdminEmail ||
                string.Equals(
                    user.UserType,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Administrator accounts cannot be suspended.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // CHECK IF ALREADY SUSPENDED
            // --------------------------------------------------------

            if (user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "This account is already suspended.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // DEFAULT REASON
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(reason))
            {
                reason =
                    "Account suspended by administrator.";
            }

            // Keep reason within the database limit
            if (reason.Length > 1000)
            {
                reason =
                    reason.Substring(0, 1000);
            }

            // --------------------------------------------------------
            // SUSPEND ACCOUNT
            // --------------------------------------------------------

            user.IsSuspended =
                true;

            user.SuspensionReason =
                reason.Trim();

            user.SuspendedAt =
                DateTime.UtcNow;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to suspend this account.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }

                return RedirectToAction(
                    nameof(Users));
            }

            TempData["SuccessMessage"] =
                $"{user.FullName} has been suspended successfully.";

            return RedirectToAction(
                nameof(Users));
        }


        // ============================================================
        // UNSUSPEND USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsuspendUser(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user account.";

                return RedirectToAction(
                    nameof(Users));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // PREVENT ADMIN ACCOUNT MODIFICATION
            // --------------------------------------------------------

            var targetIsAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");

            var targetIsAdminEmail =
                string.Equals(
                    user.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

            if (targetIsAdmin ||
                targetIsAdminEmail ||
                string.Equals(
                    user.UserType,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Administrator accounts cannot be modified here.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // CHECK CURRENT STATUS
            // --------------------------------------------------------

            if (!user.IsSuspended)
            {
                TempData["ErrorMessage"] =
                    "This account is not suspended.";

                return RedirectToAction(
                    nameof(Users));
            }

            // --------------------------------------------------------
            // RESTORE ACCOUNT
            // --------------------------------------------------------

            user.IsSuspended =
                false;

            user.SuspensionReason =
                null;

            user.SuspendedAt =
                null;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to unsuspend this account.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }

                return RedirectToAction(
                    nameof(Users));
            }

            TempData["SuccessMessage"] =
                $"{user.FullName} has been unsuspended successfully.";

            return RedirectToAction(
                nameof(Users));
        }


        // ============================================================
        // DELETE USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user.";

                return RedirectToAction(
                    nameof(Users));
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser != null &&
                currentUser.Id == id)
            {
                TempData["ErrorMessage"] =
                    "You cannot delete your own administrator account.";

                return RedirectToAction(
                    nameof(Users));
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User could not be found.";

                return RedirectToAction(
                    nameof(Users));
            }

            var result =
                await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to delete user.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(Users));
        }


        // ============================================================
        // DELETE SELLER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeller(
            string id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid seller.";

                return RedirectToAction(
                    nameof(Sellers));
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser != null &&
                currentUser.Id == id)
            {
                TempData["ErrorMessage"] =
                    "You cannot delete your own administrator account.";

                return RedirectToAction(
                    nameof(Sellers));
            }

            var seller =
                await _userManager.FindByIdAsync(id);

            if (seller == null)
            {
                TempData["ErrorMessage"] =
                    "Seller could not be found.";

                return RedirectToAction(
                    nameof(Sellers));
            }

            var result =
                await _userManager.DeleteAsync(seller);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Seller deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to delete seller.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        " " + error.Description;
                }
            }

            return RedirectToAction(
                nameof(Sellers));
        }


        // ============================================================
        // DELETE PRODUCT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(
            int id)
        {
            if (!await IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            var product =
                await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product could not be found.";

                return RedirectToAction(
                    nameof(Products));
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product deleted successfully.";

            return RedirectToAction(
                nameof(Products));
        }


        // ============================================================
        // ADMIN CHECK
        // ============================================================

        private async Task<bool> IsAdmin()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return false;
            }

            var isAdminEmail =
                string.Equals(
                    user.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

            var isAdminRole =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");

            return isAdminEmail ||
                   isAdminRole;
        }
    }
}