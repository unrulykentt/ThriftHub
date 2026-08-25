using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class EditProfileViewModel
    {
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string? FullName { get; set; }


        [Display(Name = "Country")]
        [StringLength(100)]
        public string? Country { get; set; }


        [Display(Name = "City")]
        [StringLength(100)]
        public string? City { get; set; }


        [Display(Name = "Instagram")]
        [Url]
        public string? InstagramUrl { get; set; }


        [Display(Name = "TikTok")]
        [Url]
        public string? TikTokUrl { get; set; }


        [Display(Name = "Facebook")]
        [Url]
        public string? FacebookUrl { get; set; }


        [Display(Name = "X")]
        [Url]
        public string? XUrl { get; set; }


        [Display(Name = "WhatsApp")]
        public string? WhatsAppUrl { get; set; }


        [Display(Name = "YouTube")]
        [Url]
        public string? YouTubeUrl { get; set; }


        [Display(Name = "Website")]
        [Url]
        public string? WebsiteUrl { get; set; }
    }
}