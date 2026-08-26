using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "Please enter a new password."
        )]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage =
                "The password must be at least 6 characters long."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "Please confirm your new password."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(
            "Password",
            ErrorMessage =
                "The password and confirmation password do not match."
        )]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
