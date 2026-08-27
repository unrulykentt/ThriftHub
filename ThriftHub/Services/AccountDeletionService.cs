using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services
{
    public class AccountDeletionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountDeletionService> _logger;

        public AccountDeletionService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountDeletionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<(bool Succeeded, string Message)> DeleteAccountAsync(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (false, "Invalid user.");
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return (false, "User could not be found.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds =
                    await _context.Products
                        .Where(p => p.SellerId == userId)
                        .Select(p => p.Id)
                        .ToListAsync();

                if (productIds.Count > 0)
                {
                    await _context.Favorites
                        .Where(f => productIds.Contains(f.ProductId))
                        .ExecuteDeleteAsync();

                    await _context.Products
                        .Where(p => p.SellerId == userId)
                        .ExecuteDeleteAsync();
                }

                await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .ExecuteDeleteAsync();

                await _context.Messages
                    .Where(m =>
                        m.SenderId == userId ||
                        m.RecipientId == userId)
                    .ExecuteDeleteAsync();

                await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ExecuteDeleteAsync();

                await _context.SellerSubscriptions
                    .Where(s => s.SellerId == userId)
                    .ExecuteDeleteAsync();

                await _context.Orders
                    .Where(o =>
                        o.BuyerId == userId ||
                        o.SellerId == userId)
                    .ExecuteDeleteAsync();

                await _context.BlockedUsers
                    .Where(b =>
                        b.BlockerId == userId ||
                        b.BlockedUserId == userId)
                    .ExecuteDeleteAsync();

                await _context.Reports
                    .Where(r =>
                        r.ReporterId == userId ||
                        r.ReportedUserId == userId)
                    .ExecuteDeleteAsync();

                await _context.Sellers
                    .Where(s => s.UserId == userId)
                    .ExecuteDeleteAsync();

                var deleteResult =
                    await _userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    var errors =
                        string.Join(
                            " ",
                            deleteResult.Errors.Select(
                                error => error.Description));

                    return (false, $"Unable to delete user. {errors}");
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Deleted account {UserId} ({Email}).",
                    userId,
                    user.Email);

                return (true, "User deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Failed to delete account {UserId}.",
                    userId);

                return (
                    false,
                    "Unable to delete user because related records could not be removed.");
            }
        }
    }
}
