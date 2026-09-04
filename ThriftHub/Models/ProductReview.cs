using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
