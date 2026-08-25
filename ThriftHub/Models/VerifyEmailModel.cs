using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class VerifyEmailModel
    {
        // ============================================================
        // EMAIL ADDRESS
        // ============================================================

        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;


        // ============================================================
        // VERIFICATION CODE
        // ============================================================

        [Required(ErrorMessage = "Please enter the verification code.")]
        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "The verification code must be exactly 6 digits."
        )]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "The verification code must contain exactly 6 digits."
        )]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}