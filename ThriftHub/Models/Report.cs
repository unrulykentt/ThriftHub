using System;
using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Report
    {
        public int Id { get; set; }

        // ============================================================
        // USER WHO SUBMITTED THE REPORT
        // ============================================================

        [Required]
        public string ReporterId { get; set; } = string.Empty;


        // ============================================================
        // USER BEING REPORTED
        // ============================================================

        public string? ReportedUserId { get; set; }


        // ============================================================
        // PRODUCT BEING REPORTED
        // ============================================================

        public int? ReportedProductId { get; set; }


        // ============================================================
        // REASON FOR REPORT
        // ============================================================

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;


        // ============================================================
        // ADDITIONAL DETAILS
        // ============================================================

        [MaxLength(1000)]
        public string? Description { get; set; }


        // ============================================================
        // REPORT STATUS
        // ============================================================

        // Pending, Reviewed, Resolved or Rejected

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";


        // ============================================================
        // ADMIN RESPONSE
        // ============================================================

        [MaxLength(1000)]
        public string? AdminResponse { get; set; }


        // ============================================================
        // DATE CREATED
        // ============================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // ============================================================
        // DATE REVIEWED
        // ============================================================

        public DateTime? ReviewedAt { get; set; }
    }
}