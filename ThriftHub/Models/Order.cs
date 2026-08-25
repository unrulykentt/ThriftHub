using System;
using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Order
    {
        // ============================================================
        // ORDER ID
        // ============================================================

        public int Id { get; set; }


        // ============================================================
        // PRODUCT INFORMATION
        // ============================================================

        // The product being purchased
        [Required]
        public int ProductId { get; set; }


        // ============================================================
        // BUYER AND SELLER
        // ============================================================

        // The person buying the product
        [Required]
        public string BuyerId { get; set; } = string.Empty;


        // The seller of the product
        [Required]
        public string SellerId { get; set; } = string.Empty;


        // ============================================================
        // PRICE INFORMATION
        // ============================================================

        // Original product price
        [Range(0.01, double.MaxValue)]
        public decimal ProductPrice { get; set; }


        // ThriftHub commission percentage
        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; } = 5.00m;


        // Amount ThriftHub earns from this sale
        [Range(0, double.MaxValue)]
        public decimal CommissionAmount { get; set; }


        // Amount the seller receives
        [Range(0, double.MaxValue)]
        public decimal SellerAmount { get; set; }


        // Total amount paid by the customer
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }


        // ============================================================
        // PAYMENT INFORMATION
        // ============================================================

        // Payment status
        public string PaymentStatus { get; set; } = "Pending";


        // Order status
        public string OrderStatus { get; set; } = "Pending";


        // Date the order was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Date payment was completed
        public DateTime? PaidAt { get; set; }


        // ============================================================
        // DELIVERY INFORMATION
        // ============================================================

        // Buyer's full name
        public string? FullName { get; set; }


        // Buyer's phone number
        public string? PhoneNumber { get; set; }


        // Buyer's country
        public string? Country { get; set; }


        // Buyer's city
        public string? City { get; set; }


        // Buyer's delivery address
        public string? DeliveryAddress { get; set; }
    }
}