using ThriftHub.Models;

namespace ThriftHub.Services
{
    public static class SellerVerificationRules
    {
        public static bool HasSubmittedIdentityDocuments(
            ApplicationUser user)
        {
            return
                !string.IsNullOrWhiteSpace(user.IdCardType) &&
                !string.IsNullOrWhiteSpace(user.IdCardNumber) &&
                !string.IsNullOrWhiteSpace(user.IdCardFrontUrl);
        }

        public static bool IsIdentityApproved(
            ApplicationUser user)
        {
            return
                user.IdCardVerified &&
                string.Equals(
                    user.IdCardVerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanAdminApproveSeller(
            ApplicationUser user)
        {
            return
                string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase) &&
                HasSubmittedIdentityDocuments(user) &&
                IsIdentityApproved(user);
        }

        public static bool CanSellerManageProducts(
            ApplicationUser user)
        {
            return
                string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase) &&
                user.IsVerified &&
                string.Equals(
                    user.VerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase) &&
                IsIdentityApproved(user);
        }
    }
}
