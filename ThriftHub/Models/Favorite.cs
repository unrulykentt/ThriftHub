using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        // User who favorited the product
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Product that was favorited
        [Required]
        public int ProductId { get; set; }

        // Date the favorite was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}