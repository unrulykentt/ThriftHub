using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        // ============================================================
        // SERVICES
        // ============================================================

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly PaystackService _paystackService;
        private readonly IConfiguration _configuration;


        // ============================================================
        // SUBSCRIPTION DURATION
        // ============================================================
        //
        // All paid plans are for 4 month.
        //
        // Basic    = GH₵40/month
        // Standard = GH₵80/month
        // Premium  = GH₵150/month
        //
        // ============================================================

        private const int SubscriptionDurationMonths = 4;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public SubscriptionController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            PaystackService paystackService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _paystackService = paystackService;
            _configuration = configuration;
        }


        // ============================================================
        // SUBSCRIPTION PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // CHECK ACTIVE SUBSCRIPTION
            // --------------------------------------------------------

            var activeSubscription =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == user.Id &&
                        s.Status == "Active" &&
                        s.EndDate > DateTime.UtcNow
                    )
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();


            // --------------------------------------------------------
            // SEND INFORMATION TO VIEW
            // --------------------------------------------------------

            ViewBag.ActiveSubscription =
                activeSubscription;

            ViewBag.DurationMonths =
                SubscriptionDurationMonths;


            return View();
        }


        // ============================================================
        // SELECT SUBSCRIPTION PLAN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectPlan(
            string planName,
            decimal amount)
        {
            // --------------------------------------------------------
            // GET CURRENT USER
            // --------------------------------------------------------

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // --------------------------------------------------------
            // CHECK SELLER APPROVAL
            // --------------------------------------------------------

            var isApprovedSeller =
                string.Equals(
                    user.UserType,
                    "Seller",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                user.IsVerified
                &&
                (
                    string.Equals(
                        user.VerificationStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        user.VerificationStatus,
                        "Verified",
                        StringComparison.OrdinalIgnoreCase
                    )
                );


            if (!isApprovedSeller)
            {
                TempData["ErrorMessage"] =
                    "Your seller account must be approved before you can subscribe.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // CHECK EMAIL
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["ErrorMessage"] =
                    "Please add an email address to your account before subscribing.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // CHECK PLAN NAME
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(planName))
            {
                TempData["ErrorMessage"] =
                    "Please select a subscription plan.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // AVAILABLE PLANS
            // ========================================================

            var allowedPlans =
                new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Basic",
                        40m
                    },

                    {
                        "Standard",
                        80m
                    },

                    {
                        "Premium",
                        150m
                    }
                };


            // --------------------------------------------------------
            // CHECK WHETHER PLAN EXISTS
            // --------------------------------------------------------

            if (!allowedPlans.TryGetValue(
                    planName,
                    out var correctAmount))
            {
                TempData["ErrorMessage"] =
                    "The selected subscription plan is invalid.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // ALWAYS USE SERVER-SIDE PRICE
            // ========================================================
            //
            // We do NOT trust the amount coming from the browser.
            //
            // Basic    = GH₵40
            // Standard = GH₵80
            // Premium  = GH₵150
            //
            // ========================================================

            amount = correctAmount;


            // ========================================================
            // CHECK EXISTING ACTIVE SUBSCRIPTION
            // ========================================================

            var existingSubscription =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == user.Id &&
                        s.Status == "Active" &&
                        s.EndDate > DateTime.UtcNow
                    )
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();


            if (existingSubscription != null)
            {
                TempData["SuccessMessage"] =
                    "You already have an active seller subscription.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // CREATE UNIQUE PAYSTACK REFERENCE
            // ========================================================
            //
            // No underscore is used in the reference.
            //
            // Example:
            //
            // THRIFTHUBABC123...
            //
            // ========================================================

            var reference =
                "THRIFTHUB" +
                Guid.NewGuid()
                    .ToString("N")
                    .ToUpperInvariant();


            // ========================================================
            // GET PAYSTACK CALLBACK URL
            // ========================================================

            var callbackUrl =
                _configuration[
                    "Paystack:CallbackUrl"
                ];


            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                TempData["ErrorMessage"] =
                    "Paystack callback URL is not configured.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // CREATE PENDING SUBSCRIPTION
            // ========================================================

            var subscription =
                new SellerSubscription
                {
                    SellerId =
                        user.Id,

                    PlanName =
                        planName,

                    Amount =
                        amount,

                    DurationMonths =
                        SubscriptionDurationMonths,

                    Status =
                        "Pending",

                    PaymentStatus =
                        "Pending",

                    StartDate =
                        DateTime.UtcNow,

                    EndDate =
                        DateTime.UtcNow.AddMonths(
                            SubscriptionDurationMonths
                        ),

                    CreatedAt =
                        DateTime.UtcNow,

                    PaymentReference =
                        reference,

                    PaymentMethod =
                        null,

                    IsAdminGranted =
                        false
                };


            _context.SellerSubscriptions.Add(
                subscription
            );


            await _context.SaveChangesAsync();


            // ========================================================
            // INITIALIZE PAYSTACK PAYMENT
            // ========================================================

            try
            {
                var payment =
                    await _paystackService.InitializeTransaction(
                        user.Email,
                        amount,
                        reference,
                        callbackUrl
                    );


                // ----------------------------------------------------
                // CHECK PAYSTACK RESPONSE
                // ----------------------------------------------------

                if (payment == null)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Paystack returned an empty response.";

                    return RedirectToAction(nameof(Index));
                }


                if (!payment.Status)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        string.IsNullOrWhiteSpace(
                            payment.Message)
                            ? "Unable to initialize Paystack payment."
                            : payment.Message;

                    return RedirectToAction(nameof(Index));
                }


                if (payment.Data == null)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Paystack did not return payment information.";

                    return RedirectToAction(nameof(Index));
                }


                if (string.IsNullOrWhiteSpace(
                        payment.Data.AuthorizationUrl))
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Paystack did not provide a checkout URL.";

                    return RedirectToAction(nameof(Index));
                }


                // ----------------------------------------------------
                // SAVE PAYSTACK REFERENCE
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        payment.Data.Reference))
                {
                    subscription.PaymentReference =
                        payment.Data.Reference;

                    await _context.SaveChangesAsync();
                }


                // ----------------------------------------------------
                // REDIRECT TO PAYSTACK
                // ----------------------------------------------------

                return Redirect(
                    payment.Data.AuthorizationUrl
                );
            }
            catch (Exception ex)
            {
                subscription.Status =
                    "Cancelled";

                subscription.PaymentStatus =
                    "Failed";

                await _context.SaveChangesAsync();


                TempData["ErrorMessage"] =
                    $"Unable to initialize Paystack payment: {ex.Message}";

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // PAYSTACK CALLBACK
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Callback(
            string reference)
        {
            // --------------------------------------------------------
            // CHECK REFERENCE
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(reference))
            {
                TempData["ErrorMessage"] =
                    "Payment reference was not provided.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // --------------------------------------------------------
            // FIND SUBSCRIPTION
            // --------------------------------------------------------

            var subscription =
                await _context.SellerSubscriptions
                    .FirstOrDefaultAsync(
                        s =>
                            s.PaymentReference ==
                            reference
                    );


            if (subscription == null)
            {
                TempData["ErrorMessage"] =
                    "Subscription payment could not be found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ========================================================
            // PREVENT DUPLICATE ACTIVATION
            // ========================================================

            if (string.Equals(
                    subscription.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase)
                &&
                string.Equals(
                    subscription.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["SuccessMessage"] =
                    "Your subscription is already active.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // ========================================================
            // VERIFY PAYMENT WITH PAYSTACK
            // ========================================================

            try
            {
                var verification =
                    await _paystackService
                        .VerifyTransaction(
                            reference
                        );


                // ----------------------------------------------------
                // CHECK PAYSTACK RESPONSE
                // ----------------------------------------------------

                if (verification == null)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Paystack returned an empty verification response.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                if (!verification.Status ||
                    verification.Data == null)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        string.IsNullOrWhiteSpace(
                            verification.Message)
                            ? "Payment verification failed."
                            : verification.Message;

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                // ====================================================
                // CHECK PAYMENT STATUS
                // ====================================================

                var paymentStatus =
                    verification.Data.Status;


                if (!string.Equals(
                        paymentStatus,
                        "success",
                        StringComparison.OrdinalIgnoreCase))
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Your payment was not successful.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                // ====================================================
                // CHECK CURRENCY
                // ====================================================

                if (!string.Equals(
                        verification.Data.Currency,
                        "GHS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Payment currency could not be verified.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                // ====================================================
                // CHECK PAYMENT AMOUNT
                // ====================================================
                //
                // Paystack returns the amount in pesewas.
                //
                // GH₵40  = 4000
                // GH₵80  = 8000
                // GH₵150 = 15000
                //
                // ====================================================

                var expectedAmount =
                    (long)Math.Round(
                        subscription.Amount * 100
                    );


                if (verification.Data.Amount !=
                    expectedAmount)
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Payment amount could not be verified.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                // ====================================================
                // CHECK PAYMENT REFERENCE
                // ====================================================

                if (!string.Equals(
                        verification.Data.Reference,
                        reference,
                        StringComparison.OrdinalIgnoreCase))
                {
                    subscription.Status =
                        "Cancelled";

                    subscription.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "Payment reference could not be verified.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }


                // ====================================================
                // PAYMENT SUCCESSFUL
                // ====================================================

                subscription.PaymentStatus =
                    "Paid";

                subscription.Status =
                    "Active";

                subscription.StartDate =
                    DateTime.UtcNow;

                subscription.EndDate =
                    DateTime.UtcNow.AddMonths(
                        subscription.DurationMonths
                    );

                subscription.PaymentReference =
                    reference;


                await _context.SaveChangesAsync();


                // ====================================================
                // SUCCESS MESSAGE
                // ====================================================

                TempData["SuccessMessage"] =
                    "Payment successful! Your " +
                    subscription.PlanName +
                    " seller subscription is now active.";


                // ====================================================
                // REDIRECT
                // ====================================================

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Unable to verify your payment: {ex.Message}";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }
        }
    }
}