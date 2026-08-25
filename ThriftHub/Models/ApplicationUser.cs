using Microsoft.AspNetCore.Identity;

namespace ThriftHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ============================================================
        // PERSONAL INFORMATION
        // ============================================================

        public string? FullName { get; set; }

        public string? Country { get; set; }

        public string? City { get; set; }


        // ============================================================
        // USER TYPE
        // ============================================================

        // Customer, Seller or Admin
        public string UserType { get; set; } = "Customer";


        // ============================================================
        // SELLER / ACCOUNT VERIFICATION
        // ============================================================

        public string VerificationStatus { get; set; } = "NotSubmitted";

        public bool IsVerified { get; set; } = false;


        // ============================================================
        // IDENTITY VERIFICATION
        // ============================================================

        // Type of government-issued identification.
        //
        // Examples:
        // Ghana Card
        // Passport
        // Driver's License
        // Voter ID
        //
        public string? IdCardType { get; set; }


        // ID card number.
        //
        // This information is private and must NOT be displayed
        // publicly on the user's profile.
        //
        public string? IdCardNumber { get; set; }


        // ============================================================
        // ID CARD DOCUMENTS
        // ============================================================

        // Private path to the front of the ID card.
        public string? IdCardFrontUrl { get; set; }


        // Private path to the back of the ID card.
        //
        // Some IDs may only require one side.
        //
        public string? IdCardBackUrl { get; set; }


        // ============================================================
        // ID CARD VERIFICATION STATUS
        // ============================================================

        // false = not yet verified by administrator
        // true  = administrator has verified the ID
        //
        public bool IdCardVerified { get; set; } = false;


        // ============================================================
        // SELLER SUBSCRIPTION ACCESS
        // ============================================================

        // If true, the administrator has allowed this seller
        // to sell without paying for a subscription.
        //
        public bool SubscriptionWaived { get; set; } = false;


        // ============================================================
        // EMAIL VERIFICATION
        // ============================================================

        public string? EmailVerificationCode { get; set; }

        public DateTime? EmailVerificationCodeExpiresAt { get; set; }


        // ============================================================
        // PROFILE PICTURE
        // ============================================================

        public string? ProfileImageUrl { get; set; }


        // ============================================================
        // ONLINE STATUS
        // ============================================================

        public bool IsOnline { get; set; } = false;


        // ============================================================
        // SOCIAL MEDIA
        // ============================================================

        public string? InstagramUrl { get; set; }

        public string? TikTokUrl { get; set; }

        public string? FacebookUrl { get; set; }

        public string? XUrl { get; set; }

        public string? WhatsAppUrl { get; set; }

        public string? YouTubeUrl { get; set; }

        public string? WebsiteUrl { get; set; }


        // ============================================================
        // ACCOUNT SUSPENSION
        // ============================================================

        // True when an administrator has suspended this account.
        public bool IsSuspended { get; set; } = false;


        // Date and time the account was suspended.
        public DateTime? SuspendedAt { get; set; }


        // Reason provided by the administrator
        // when the account was suspended.
        public string? SuspensionReason { get; set; }


        // ============================================================
        // ACCOUNT DATE
        // ============================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}