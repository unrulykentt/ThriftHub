using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class ChangePasswordViewModel
    {
        // ============================================================
        // CURRENT PASSWORD
        // ============================================================

        [Required(
            ErrorMessage = "Please enter your current password."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;


        // ============================================================
        // NEW PASSWORD
        // ============================================================

        [Required(
            ErrorMessage = "Please enter a new password."
        )]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage =
                "The new password must be at least 6 characters long."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;


        // ============================================================
        // CONFIRM PASSWORD
        // ============================================================

        [Required(
            ErrorMessage = "Please confirm your new password."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(
            "NewPassword",
            ErrorMessage =
                "The new password and confirmation password do not match."
        )]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}