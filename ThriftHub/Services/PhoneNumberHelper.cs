using System.Text.RegularExpressions;

namespace ThriftHub.Services
{
    public static class PhoneNumberHelper
    {
        public static string? NormalizeToE164(
            string? phoneNumber,
            string? country = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var digits =
                Regex.Replace(
                    phoneNumber.Trim(),
                    @"[^\d+]",
                    string.Empty);

            if (digits.StartsWith('+'))
            {
                return digits.Length >= 10
                    ? digits
                    : null;
            }

            if (digits.StartsWith("00"))
            {
                return "+" + digits[2..];
            }

            var countryName =
                country?.Trim() ?? string.Empty;

            if (
                countryName.Contains(
                    "Ghana",
                    StringComparison.OrdinalIgnoreCase) ||
                countryName.Equals(
                    "GH",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (digits.StartsWith("233"))
                {
                    return "+" + digits;
                }

                if (digits.StartsWith('0') &&
                    digits.Length == 10)
                {
                    return "+233" + digits[1..];
                }

                if (digits.Length == 9)
                {
                    return "+233" + digits;
                }
            }

            if (digits.Length >= 10 &&
                digits.Length <= 15)
            {
                return "+" + digits;
            }

            return null;
        }
    }
}
