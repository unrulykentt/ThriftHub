using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ThriftHub.Models
{
    public class RegisterViewModel
    {
        // ============================================================
        // PERSONAL INFORMATION
        // ============================================================

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;


        // ============================================================
        // ACCOUNT INFORMATION
        // ============================================================

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;


        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;


        [Required]
        public string Country { get; set; } = string.Empty;


        [Required]
        public string City { get; set; } = string.Empty;


        // ============================================================
        // ACCOUNT TYPE
        // ============================================================

        [Required]
        [Display(Name = "Account Type")]
        public string UserType { get; set; } = "Customer";


        // ============================================================
        // IDENTITY VERIFICATION
        // ============================================================

        [Required]
        [Display(Name = "ID Type")]
        public string IdCardType { get; set; } = string.Empty;


        [Required]
        [Display(Name = "ID Card Number")]
        public string IdCardNumber { get; set; } = string.Empty;


        // ============================================================
        // ID CARD FRONT
        // ============================================================

        [Required]
        [Display(Name = "ID Card Front")]
        public IFormFile? IdCardFront { get; set; }


        // ============================================================
        // ID CARD BACK
        // ============================================================
        //
        // Optional because some identification documents may
        // only require one side.
        //
        public IFormFile? IdCardBack { get; set; }


        // ============================================================
        // PASSWORD
        // ============================================================

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;


        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}