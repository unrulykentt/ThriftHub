using System;
using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? Subcategory { get; set; }

        public string? Condition { get; set; }

        // ============================================================
        // PRODUCT SIZES
        // ============================================================

        // Examples:
        // S,M,L,XL
        // 38,39,40,41,42
        // 6,7,8,9
        // One Size

        public string? Sizes { get; set; }

        public string? ImageUrl { get; set; }

        // false = Available
        // true = Sold

        public bool IsSold { get; set; } = false;

        public string? SellerId { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}