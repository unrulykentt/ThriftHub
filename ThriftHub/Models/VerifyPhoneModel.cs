using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class VerifyPhoneModel
    {
        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please enter the verification code.")]
        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "The verification code must be exactly 6 digits.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "The verification code must contain exactly 6 digits.")]
        [Display(Name = "WhatsApp Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}
