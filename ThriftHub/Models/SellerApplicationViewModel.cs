using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ThriftHub.Models
{
    public class SellerApplicationViewModel
    {
        [Required(ErrorMessage = "Please select your ID type.")]
        [Display(Name = "ID Type")]
        public string IdCardType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your ID card number.")]
        [Display(Name = "ID Card Number")]
        public string IdCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload the front of your ID.")]
        [Display(Name = "ID Card Front")]
        public IFormFile? IdCardFront { get; set; }

        [Display(Name = "ID Card Back")]
        public IFormFile? IdCardBack { get; set; }
    }
}
