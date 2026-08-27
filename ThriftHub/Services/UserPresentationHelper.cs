using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services
{
    public static class UserPresentationHelper
    {
        public static string GetDisplayName(
            ApplicationUser? user)
        {
            if (user == null)
            {
                return "User";
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                return user.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.UserName) &&
                !user.UserName.Contains('@'))
            {
                return user.UserName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.Email) &&
                user.Email.Contains('@'))
            {
                var localPart =
                    user.Email
                        .Split('@')[0]
                        .Replace('.', ' ')
                        .Replace('_', ' ')
                        .Trim();

                if (!string.IsNullOrWhiteSpace(localPart))
                {
                    return ToTitleCase(localPart);
                }
            }

            return "User";
        }

        public static Dictionary<string, string> BuildSellerDisplayNameMap(
            IEnumerable<ApplicationUser> sellers)
        {
            return sellers
                .Where(seller =>
                    !string.IsNullOrWhiteSpace(seller.Id))
                .GroupBy(
                    seller => seller.Id,
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => GetDisplayName(group.First()),
                    StringComparer.Ordinal);
        }

        public static async Task<Dictionary<string, string>> LoadSellerDisplayNamesAsync(
            ApplicationDbContext context,
            IEnumerable<Product> products)
        {
            var sellerIds =
                products
                    .Select(product => product.SellerId)
                    .Where(id =>
                        !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

            if (sellerIds.Count == 0)
            {
                return new Dictionary<string, string>(
                    StringComparer.Ordinal);
            }

            var sellers =
                await context.Users
                    .AsNoTracking()
                    .Where(user =>
                        sellerIds.Contains(user.Id))
                    .ToListAsync();

            return BuildSellerDisplayNameMap(sellers);
        }

        public static string GetSellerDisplayName(
            Product product,
            IReadOnlyDictionary<string, string>? sellerDisplayNames)
        {
            if (
                !string.IsNullOrWhiteSpace(product.SellerId) &&
                sellerDisplayNames != null &&
                sellerDisplayNames.TryGetValue(
                    product.SellerId,
                    out var sellerName))
            {
                return sellerName;
            }

            return "Seller";
        }

        public static string GetInitials(
            string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "U";
            }

            var parts =
                displayName
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                return (
                    char.ToUpperInvariant(parts[0][0])
                    .ToString()
                    + char.ToUpperInvariant(
                        parts[^1][0])
                );
            }

            return char.ToUpperInvariant(
                displayName.Trim()[0])
                .ToString();
        }

        public static bool HasProfilePhoto(
            string? profileImageUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    profileImageUrl))
            {
                return false;
            }

            return !profileImageUrl.Contains(
                "default-avatar",
                StringComparison.OrdinalIgnoreCase);
        }

        public static string? ResolveProfileImageUrl(
            string? profileImageUrl)
        {
            return HasProfilePhoto(profileImageUrl)
                ? profileImageUrl
                : null;
        }

        public static string GetRoleLabel(
            string? userType)
        {
            return string.Equals(
                userType,
                "Seller",
                StringComparison.OrdinalIgnoreCase)
                ? "Seller"
                : "Buyer";
        }

        private static string ToTitleCase(
            string value)
        {
            var parts =
                value
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            return string.Join(
                " ",
                parts.Select(
                    part =>
                        part.Length == 0
                            ? part
                            : char.ToUpperInvariant(
                                part[0])
                            + (
                                part.Length > 1
                                    ? part[1..]
                                        .ToLowerInvariant()
                                    : string.Empty)));
        }
    }
}
