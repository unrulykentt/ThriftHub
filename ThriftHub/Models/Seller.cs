using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Seller
    {
        public int Id { get; set; }


        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;


        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;


        public string ProfileImageUrl { get; set; } = string.Empty;


        public bool IsOnline { get; set; }


        public bool IsVerified { get; set; }


        public string VerificationStatus { get; set; } = "Pending";


        public string UserId { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}