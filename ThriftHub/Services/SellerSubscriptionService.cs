using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services
{
    public class SellerSubscriptionService
    {
        public const string WelcomeTrialPlanName =
            "Welcome Trial";

        public const int WelcomeTrialMonths =
            1;

        private readonly ApplicationDbContext _context;

        public SellerSubscriptionService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public static bool IsWelcomeTrial(
            SellerSubscription subscription)
        {
            return string.Equals(
                subscription.PlanName,
                WelcomeTrialPlanName,
                StringComparison.OrdinalIgnoreCase)
                &&
                string.Equals(
                    subscription.PaymentStatus,
                    "Trial",
                    StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSubscriptionActive(
            SellerSubscription subscription,
            DateTime utcNow)
        {
            if (!string.Equals(
                    subscription.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (subscription.EndDate <= utcNow)
            {
                return false;
            }

            if (subscription.IsAdminGranted)
            {
                return true;
            }

            if (IsWelcomeTrial(subscription))
            {
                return true;
            }

            return string.Equals(
                subscription.PaymentStatus,
                "Paid",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task ExpireEndedSubscriptionsAsync(
            string sellerId)
        {
            var now =
                DateTime.UtcNow;

            var expired =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == sellerId &&
                        s.Status == "Active" &&
                        s.EndDate <= now)
                    .ToListAsync();

            if (!expired.Any())
            {
                return;
            }

            foreach (var subscription in expired)
            {
                subscription.Status =
                    "Expired";
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveSubscriptionAsync(
            string sellerId)
        {
            await ExpireEndedSubscriptionsAsync(
                sellerId);

            var now =
                DateTime.UtcNow;

            var subscriptions =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == sellerId &&
                        s.Status == "Active" &&
                        s.EndDate > now)
                    .ToListAsync();

            return subscriptions.Any(
                s =>
                    IsSubscriptionActive(
                        s,
                        now));
        }

        public async Task<SellerSubscription?>
            GetActiveSubscriptionAsync(
                string sellerId)
        {
            await ExpireEndedSubscriptionsAsync(
                sellerId);

            var now =
                DateTime.UtcNow;

            var subscriptions =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == sellerId &&
                        s.Status == "Active" &&
                        s.EndDate > now)
                    .OrderByDescending(
                        s => s.EndDate)
                    .ToListAsync();

            return subscriptions.FirstOrDefault(
                s =>
                    IsSubscriptionActive(
                        s,
                        now));
        }

        public async Task<bool> HasPaidActiveSubscriptionAsync(
            string sellerId)
        {
            var active =
                await GetActiveSubscriptionAsync(
                    sellerId);

            if (active == null)
            {
                return false;
            }

            return !IsWelcomeTrial(active);
        }

        public async Task<SellerSubscription?>
            GetWelcomeTrialAsync(
                string sellerId)
        {
            return await _context.SellerSubscriptions
                .Where(s =>
                    s.SellerId == sellerId &&
                    s.PlanName == WelcomeTrialPlanName)
                .OrderByDescending(
                    s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task GrantWelcomeTrialAsync(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var alreadyGranted =
                await _context.SellerSubscriptions
                    .AnyAsync(s =>
                        s.SellerId == userId &&
                        s.PlanName ==
                            WelcomeTrialPlanName);

            if (alreadyGranted)
            {
                return;
            }

            var now =
                DateTime.UtcNow;

            var trial =
                new SellerSubscription
                {
                    SellerId =
                        userId,

                    PlanName =
                        WelcomeTrialPlanName,

                    Amount =
                        0m,

                    DurationMonths =
                        WelcomeTrialMonths,

                    Status =
                        "Active",

                    PaymentStatus =
                        "Trial",

                    StartDate =
                        now,

                    EndDate =
                        now.AddMonths(
                            WelcomeTrialMonths),

                    CreatedAt =
                        now,

                    IsAdminGranted =
                        false
                };

            _context.SellerSubscriptions.Add(
                trial);

            await _context.SaveChangesAsync();
        }

        public async Task EndWelcomeTrialsAsync(
            string sellerId)
        {
            var trials =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == sellerId &&
                        s.PlanName ==
                            WelcomeTrialPlanName &&
                        s.Status == "Active")
                    .ToListAsync();

            foreach (var trial in trials)
            {
                trial.Status =
                    "Expired";
            }

            if (trials.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public static int GetDaysRemaining(
            SellerSubscription subscription,
            DateTime utcNow)
        {
            var remaining =
                subscription.EndDate - utcNow;

            if (remaining.TotalDays <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(
                remaining.TotalDays);
        }
    }
}
