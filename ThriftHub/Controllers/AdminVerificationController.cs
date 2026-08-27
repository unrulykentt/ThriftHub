using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminVerificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminVerificationController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        // ============================================================
        // ADMIN VERIFICATION DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users =
                await _userManager.Users
                    .Where(u =>
                        u.UserType != "Admin" &&
                        u.VerificationStatus != "Approved" &&
                        u.VerificationStatus != "Rejected")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

            return View(users);
        }


        // ============================================================
        // APPROVE ACCOUNT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user ID.";

                return RedirectToAction(nameof(Index));
            }


            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User account not found.";

                return RedirectToAction(nameof(Index));
            }


            if (user.UserType == "Admin")
            {
                TempData["ErrorMessage"] =
                    "Admin accounts cannot be processed here.";

                return RedirectToAction(nameof(Index));
            }


            if (string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase) &&
                !SellerVerificationRules.IsIdentityApproved(user))
            {
                TempData["ErrorMessage"] =
                    "Seller accounts cannot be approved until their government ID has been verified.";

                return RedirectToAction(nameof(Index));
            }

            user.IsVerified = true;

            user.VerificationStatus = "Approved";


            var result =
                await _userManager.UpdateAsync(user);


            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    $"{user.FullName}'s account has been approved.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "The account could not be approved.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        $" {error.Description}";
                }
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // REJECT ACCOUNT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] =
                    "Invalid user ID.";

                return RedirectToAction(nameof(Index));
            }


            var user =
                await _userManager.FindByIdAsync(id);


            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "User account not found.";

                return RedirectToAction(nameof(Index));
            }


            if (user.UserType == "Admin")
            {
                TempData["ErrorMessage"] =
                    "Admin accounts cannot be rejected.";

                return RedirectToAction(nameof(Index));
            }


            user.IsVerified = false;

            user.VerificationStatus = "Rejected";


            var result =
                await _userManager.UpdateAsync(user);


            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    $"{user.FullName}'s account has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "The account could not be rejected.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        $" {error.Description}";
                }
            }


            return RedirectToAction(nameof(Index));
        }
    }
}