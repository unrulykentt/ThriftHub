using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ============================================================
        // NOTIFICATIONS
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


            var notifications =
                await _context.Set<Notification>()
                    .AsNoTracking()
                    .Where(n =>
                        n.UserId == user.Id)
                    .OrderByDescending(
                        n => n.CreatedAt)
                    .ToListAsync();


            return View(notifications);
        }


        // ============================================================
        // MARK ONE NOTIFICATION AS READ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var notification =
                await _context.Set<Notification>()
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.UserId == user.Id);


            if (notification == null)
            {
                return NotFound();
            }


            notification.IsRead = true;

            await _context.SaveChangesAsync();


            if (!string.IsNullOrWhiteSpace(
                notification.Link))
            {
                return Redirect(
                    notification.Link);
            }


            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // MARK ALL AS READ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var notifications =
                await _context.Set<Notification>()
                    .Where(n =>
                        n.UserId == user.Id &&
                        !n.IsRead)
                    .ToListAsync();


            foreach (var notification
                     in notifications)
            {
                notification.IsRead = true;
            }


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "All notifications have been marked as read.";


            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // DELETE NOTIFICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var notification =
                await _context.Set<Notification>()
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.UserId == user.Id);


            if (notification == null)
            {
                return NotFound();
            }


            _context.Set<Notification>()
                .Remove(notification);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Notification deleted.";


            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // DELETE ALL NOTIFICATIONS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var notifications =
                await _context.Set<Notification>()
                    .Where(n =>
                        n.UserId == user.Id)
                    .ToListAsync();


            if (notifications.Any())
            {
                _context.Set<Notification>()
                    .RemoveRange(notifications);

                await _context.SaveChangesAsync();
            }


            TempData["SuccessMessage"] =
                "All notifications have been deleted.";


            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // UNREAD NOTIFICATION COUNT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(new
                {
                    count = 0
                });
            }


            var count =
                await _context.Set<Notification>()
                    .CountAsync(n =>
                        n.UserId == user.Id &&
                        !n.IsRead);


            return Json(new
            {
                count
            });
        }
    }
}