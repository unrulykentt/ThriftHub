using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services;

public class ProductViewService
{
    private const string AnonymousViewerSessionKey =
        "ThriftHub:ViewerKey";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductViewService> _logger;

    public ProductViewService(
        ApplicationDbContext context,
        ILogger<ProductViewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public static string FormatViewCount(int viewCount)
    {
        return viewCount == 1
            ? "1 view"
            : $"{viewCount:N0} views";
    }

    public async Task RecordViewAsync(
        Product product,
        HttpContext httpContext,
        string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId) &&
            string.Equals(
                userId,
                product.SellerId,
                StringComparison.Ordinal))
        {
            return;
        }

        var viewerKey =
            GetViewerKey(httpContext, userId);

        if (string.IsNullOrWhiteSpace(viewerKey))
        {
            return;
        }

        var alreadyViewed =
            await _context.ProductViews
                .AsNoTracking()
                .AnyAsync(view =>
                    view.ProductId == product.Id &&
                    view.ViewerKey == viewerKey);

        if (alreadyViewed)
        {
            return;
        }

        _context.ProductViews.Add(
            new ProductView
            {
                ProductId = product.Id,
                ViewerKey = viewerKey,
                FirstViewedAt = DateTime.UtcNow
            });

        product.ViewCount++;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(
                ex,
                "Skipped duplicate product view for product {ProductId}.",
                product.Id);

            _context.Entry(product).State = EntityState.Unchanged;

            var trackedView =
                _context.ChangeTracker
                    .Entries<ProductView>()
                    .Select(entry => entry.Entity)
                    .FirstOrDefault(view =>
                        view.ProductId == product.Id &&
                        view.ViewerKey == viewerKey);

            if (trackedView != null)
            {
                _context.Entry(trackedView).State = EntityState.Detached;
            }

            product.ViewCount =
                await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == product.Id)
                    .Select(p => p.ViewCount)
                    .FirstOrDefaultAsync();
        }
    }

    private static string? GetViewerKey(
        HttpContext httpContext,
        string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var session = httpContext.Session;

        if (!session.IsAvailable)
        {
            return null;
        }

        var viewerKey =
            session.GetString(AnonymousViewerSessionKey);

        if (string.IsNullOrWhiteSpace(viewerKey))
        {
            viewerKey = $"anon:{Guid.NewGuid():N}";
            session.SetString(
                AnonymousViewerSessionKey,
                viewerKey);
        }

        return viewerKey;
    }
}
