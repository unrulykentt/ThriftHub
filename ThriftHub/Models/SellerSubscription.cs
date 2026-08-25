using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThriftHub.Models
{
    public class SellerSubscription
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        public int Id { get; set; }


        // ============================================================
        // SELLER
        // ============================================================

        [Required]
        public string SellerId { get; set; } = string.Empty;


        // ============================================================
        // SUBSCRIPTION PLAN
        // ============================================================

        [Required]
        [MaxLength(50)]
        public string PlanName { get; set; } = string.Empty;


        // ============================================================
        // AMOUNT
        // ============================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }


        // ============================================================
        // DURATION
        // ============================================================

        public int DurationMonths { get; set; } = 4;


        // ============================================================
        // SUBSCRIPTION STATUS
        // ============================================================

        // Pending
        // Active
        // Expired
        // Cancelled
        public string Status { get; set; } = "Pending";


        // ============================================================
        // PAYMENT STATUS
        // ============================================================

        // Pending
        // Paid
        // Failed
        // Cancelled
        public string PaymentStatus { get; set; } = "Pending";


        // ============================================================
        // DATES
        // ============================================================

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // ============================================================
        // PAYMENT REFERENCE
        // ============================================================

        // Used later for Mobile Money/card payment references.
        public string? PaymentReference { get; set; }


        // ============================================================
        // PAYMENT METHOD
        // ============================================================

        // Examples:
        // Mobile Money
        // Card
        // Bank
        public string? PaymentMethod { get; set; }


        // ============================================================
        // ADMIN FREE SUBSCRIPTION
        // ============================================================

        // True when the administrator granted this subscription
        // without requiring the seller to make a payment.
        public bool IsAdminGranted { get; set; } = false;
    }
}