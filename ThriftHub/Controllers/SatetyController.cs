using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class SafetyController : Controller
    {
        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public SafetyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                       "Admin");
        }


        // ============================================================
        // BLOCKED USERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> BlockedUsers()
        {
            var user = await GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // GET USERS BLOCKED BY CURRENT USER
            // --------------------------------------------------------

            var blockedUsers =
                await _context.BlockedUsers
                    .Where(b =>
                        b.BlockerId == user.Id)
                    .OrderByDescending(
                        b => b.CreatedAt)
                    .ToListAsync();


            // --------------------------------------------------------
            // GET THE USER ACCOUNTS
            // --------------------------------------------------------

            var blockedUserIds =
                blockedUsers
                    .Select(b => b.BlockedUserId)
                    .ToList();


            var users =
                await _context.Users
                    .Where(u =>
                        blockedUserIds.Contains(u.Id))
                    .ToListAsync();


            ViewBag.Users = users;


            return View(blockedUsers);
        }


        // ============================================================
        // BLOCK USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(
            string userId)
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] =
                    "Invalid user.";

                return RedirectToAction(
                    nameof(BlockedUsers));
            }


            // --------------------------------------------------------
            // CANNOT BLOCK YOURSELF
            // --------------------------------------------------------

            if (currentUser.Id == userId)
            {
                TempData["ErrorMessage"] =
                    "You cannot block yourself.";

                return RedirectToAction(
                    nameof(BlockedUsers));
            }


            // --------------------------------------------------------
            // CHECK USER EXISTS
            // --------------------------------------------------------

            var userToBlock =
                await _userManager.FindByIdAsync(
                    userId);

            if (userToBlock == null)
            {
                TempData["ErrorMessage"] =
                    "User not found.";

                return RedirectToAction(
                    nameof(BlockedUsers));
            }


            // --------------------------------------------------------
            // CHECK IF ALREADY BLOCKED
            // --------------------------------------------------------

            var alreadyBlocked =
                await _context.BlockedUsers
                    .AnyAsync(b =>
                        b.BlockerId == currentUser.Id &&
                        b.BlockedUserId == userId);


            if (alreadyBlocked)
            {
                TempData["ErrorMessage"] =
                    "You have already blocked this user.";

                return RedirectToAction(
                    nameof(BlockedUsers));
            }


            // --------------------------------------------------------
            // CREATE BLOCK
            // --------------------------------------------------------

            var blockedUser =
                new BlockedUser
                {
                    BlockerId = currentUser.Id,
                    BlockedUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };


            _context.BlockedUsers.Add(
                blockedUser);


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "User has been blocked successfully.";


            return RedirectToAction(
                nameof(BlockedUsers));
        }


        // ============================================================
        // UNBLOCK USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(
            int id)
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // FIND BLOCK RECORD
            // --------------------------------------------------------

            var blocked =
                await _context.BlockedUsers
                    .FirstOrDefaultAsync(b =>
                        b.Id == id &&
                        b.BlockerId == currentUser.Id);


            if (blocked == null)
            {
                TempData["ErrorMessage"] =
                    "Blocked user record was not found.";

                return RedirectToAction(
                    nameof(BlockedUsers));
            }


            // --------------------------------------------------------
            // REMOVE BLOCK
            // --------------------------------------------------------

            _context.BlockedUsers.Remove(
                blocked);


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "User has been unblocked successfully.";


            return RedirectToAction(
                nameof(BlockedUsers));
        }


        // ============================================================
        // MY REPORTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyReports()
        {
            var user = await GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // GET CURRENT USER'S REPORTS
            // --------------------------------------------------------

            var reports =
                await _context.Reports
                    .Where(r =>
                        r.ReporterId == user.Id)
                    .OrderByDescending(
                        r => r.CreatedAt)
                    .ToListAsync();


            return View(reports);
        }


        // ============================================================
        // REPORT USER - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ReportUser(
            string? userId)
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // PREVENT REPORTING YOURSELF
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(userId) &&
                currentUser.Id == userId)
            {
                TempData["ErrorMessage"] =
                    "You cannot report yourself.";

                return RedirectToAction(
                    nameof(MyReports));
            }


            // --------------------------------------------------------
            // PASS REPORTED USER ID TO VIEW
            // --------------------------------------------------------

            ViewBag.ReportedUserId =
                userId;


            return View();
        }


        // ============================================================
        // REPORT USER - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportUser(
            Report model)
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // SET REPORTER
            // --------------------------------------------------------

            model.ReporterId =
                currentUser.Id;


            // --------------------------------------------------------
            // VALIDATE REPORTED USER
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    model.ReportedUserId))
            {
                var reportedUser =
                    await _userManager.FindByIdAsync(
                        model.ReportedUserId);


                if (reportedUser == null)
                {
                    ModelState.AddModelError(
                        "ReportedUserId",
                        "The reported user could not be found.");
                }


                if (model.ReportedUserId ==
                    currentUser.Id)
                {
                    ModelState.AddModelError(
                        "ReportedUserId",
                        "You cannot report yourself.");
                }
            }


            // --------------------------------------------------------
            // VALIDATE PRODUCT IF PROVIDED
            // --------------------------------------------------------

            if (model.ReportedProductId.HasValue)
            {
                var productExists =
                    await _context.Products
                        .AnyAsync(p =>
                            p.Id ==
                            model.ReportedProductId.Value);


                if (!productExists)
                {
                    ModelState.AddModelError(
                        "ReportedProductId",
                        "The reported product could not be found.");
                }
            }


            // --------------------------------------------------------
            // MODEL VALIDATION
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // --------------------------------------------------------
            // REPORT DETAILS
            // --------------------------------------------------------

            model.Status =
                "Pending";

            model.AdminResponse =
                null;

            model.CreatedAt =
                DateTime.UtcNow;

            model.ReviewedAt =
                null;


            // --------------------------------------------------------
            // SAVE REPORT
            // --------------------------------------------------------

            _context.Reports.Add(
                model);


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your report has been submitted successfully. Our administrators will review it.";


            return RedirectToAction(
                nameof(MyReports));
        }


        // ============================================================
        // ADMIN REPORTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AdminReports()
        {
            var user = await GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ADMIN CHECK
            // --------------------------------------------------------

            if (!await IsAdmin(user))
            {
                TempData["ErrorMessage"] =
                    "You do not have permission to view administrator reports.";

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // --------------------------------------------------------
            // GET ALL REPORTS
            // --------------------------------------------------------

            var reports =
                await _context.Reports
                    .OrderByDescending(
                        r => r.CreatedAt)
                    .ToListAsync();


            return View(reports);
        }


        // ============================================================
        // REVIEW REPORT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ReviewReport(
            int id)
        {
            var user = await GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            if (!await IsAdmin(user))
            {
                TempData["ErrorMessage"] =
                    "You do not have permission to review reports.";

                return RedirectToAction(
                    nameof(MyReports));
            }


            var report =
                await _context.Reports
                    .FirstOrDefaultAsync(
                        r => r.Id == id);


            if (report == null)
            {
                TempData["ErrorMessage"] =
                    "Report not found.";

                return RedirectToAction(
                    nameof(AdminReports));
            }


            return View(report);
        }


        // ============================================================
        // REVIEW REPORT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewReport(
            int id,
            string status,
            string? adminResponse)
        {
            var user = await GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // --------------------------------------------------------
            // ADMIN CHECK
            // --------------------------------------------------------

            if (!await IsAdmin(user))
            {
                TempData["ErrorMessage"] =
                    "You do not have permission to review reports.";

                return RedirectToAction(
                    nameof(MyReports));
            }


            // --------------------------------------------------------
            // FIND REPORT
            // --------------------------------------------------------

            var report =
                await _context.Reports
                    .FirstOrDefaultAsync(
                        r => r.Id == id);


            if (report == null)
            {
                TempData["ErrorMessage"] =
                    "Report not found.";

                return RedirectToAction(
                    nameof(AdminReports));
            }


            // --------------------------------------------------------
            // VALID STATUS
            // --------------------------------------------------------

            var validStatuses =
                new[]
                {
                    "Pending",
                    "Reviewed",
                    "Resolved",
                    "Rejected"
                };


            if (!validStatuses.Contains(
                    status,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Invalid report status.";

                return RedirectToAction(
                    nameof(AdminReports));
            }


            // --------------------------------------------------------
            // UPDATE REPORT
            // --------------------------------------------------------

            report.Status =
                status;

            report.AdminResponse =
                string.IsNullOrWhiteSpace(
                    adminResponse)
                    ? null
                    : adminResponse.Trim();

            report.ReviewedAt =
                DateTime.UtcNow;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Report has been updated successfully.";


            return RedirectToAction(
                nameof(AdminReports));
        }
    }
}