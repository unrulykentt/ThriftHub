using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services;

public class ProductImageService
{
    public const int MinImagesPerProduct = 5;

    public const int MaxImagesPerProduct = 8;

    private static readonly string[] AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp"
    ];

    private readonly ApplicationDbContext _context;
    private readonly AppStorageService _storage;

    public ProductImageService(
        ApplicationDbContext context,
        AppStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<List<ProductImage>> GetProductImagesAsync(
        int productId)
    {
        return await _context.ProductImages
            .AsNoTracking()
            .Where(image => image.ProductId == productId)
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .ToListAsync();
    }

    public async Task<List<string>> GetProductImageUrlsAsync(
        int productId,
        string? fallbackImageUrl)
    {
        var images =
            await GetProductImagesAsync(productId);

        if (images.Count > 0)
        {
            return images
                .Select(image => image.ImageUrl)
                .ToList();
        }

        return string.IsNullOrWhiteSpace(fallbackImageUrl)
            ? []
            : [fallbackImageUrl];
    }

    public async Task<List<string>> SaveProductImagesAsync(
        IEnumerable<IFormFile> files)
    {
        var savedUrls = new List<string>();

        foreach (var file in files)
        {
            if (file == null || file.Length <= 0)
            {
                continue;
            }

            if (savedUrls.Count >= MaxImagesPerProduct)
            {
                break;
            }

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            const long maxFileSize = 10 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                throw new InvalidOperationException(
                    "Each product image must be smaller than 10 MB.");
            }

            var uploadsFolder =
                _storage.GetUploadsCategoryPath("products");

            Directory.CreateDirectory(uploadsFolder);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath =
                Path.Combine(uploadsFolder, fileName);

            await using (
                var stream = new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            savedUrls.Add(
                _storage.BuildUploadsWebPath(
                    "products",
                    fileName));
        }

        return savedUrls;
    }

    public async Task AddImagesToProductAsync(
        int productId,
        IReadOnlyList<string> imageUrls)
    {
        if (imageUrls.Count == 0)
        {
            return;
        }

        var sortOrder = 0;

        foreach (var imageUrl in imageUrls)
        {
            _context.ProductImages.Add(
                new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = imageUrl,
                    SortOrder = sortOrder++
                });
        }

        await _context.SaveChangesAsync();
    }
}
