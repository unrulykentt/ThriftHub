using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int productId,
        int rating,
        string? comment)
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        var product =
            await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == productId);

        if (product == null)
        {
            return NotFound();
        }

        if (product.SellerId == user.Id)
        {
            TempData["ErrorMessage"] =
                "You cannot review your own listing.";

            return RedirectToAction(
                "Details",
                "MarketPlace",
                new { id = productId });
        }

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] =
                "Please choose a rating between 1 and 5 stars.";

            return RedirectToAction(
                "Details",
                "MarketPlace",
                new { id = productId });
        }

        comment =
            string.IsNullOrWhiteSpace(comment)
                ? null
                : comment.Trim();

        if (comment != null && comment.Length > 1000)
        {
            comment = comment[..1000];
        }

        var existingReview =
            await _context.ProductReviews
                .FirstOrDefaultAsync(review =>
                    review.ProductId == productId &&
                    review.UserId == user.Id);

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Comment = comment;
            existingReview.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ProductReviews.Add(
                new ProductReview
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            existingReview != null
                ? "Your review has been updated."
                : "Thank you for your review!";

        return RedirectToAction(
            "Details",
            "MarketPlace",
            new { id = productId });
    }
}
