using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class UpdateDisplayNameViewModel
    {
        [Display(Name = "Your Name")]
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string FullName { get; set; } = string.Empty;
    }
}
