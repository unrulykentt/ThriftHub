using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // ThriftHub commission
        private const decimal CommissionPercentage = 5.00m;

        public OrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // CHECKOUT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Checkout(int productId)
        {
            var buyer = await _userManager.GetUserAsync(User);

            if (buyer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "The product could not be found.";

                return RedirectToAction("Index", "Marketplace");
            }

            // Product already sold
            if (product.IsSold)
            {
                TempData["ErrorMessage"] =
                    "This product has already been sold.";

                return RedirectToAction(
                    "Details",
                    "Marketplace",
                    new { id = product.Id });
            }

            // Seller must exist
            if (string.IsNullOrWhiteSpace(product.SellerId))
            {
                TempData["ErrorMessage"] =
                    "This product does not have a valid seller.";

                return RedirectToAction(
                    "Details",
                    "Marketplace",
                    new { id = product.Id });
            }

            // Seller cannot buy own product
            if (product.SellerId == buyer.Id)
            {
                TempData["ErrorMessage"] =
                    "You cannot purchase your own product.";

                return RedirectToAction(
                    "Details",
                    "Marketplace",
                    new { id = product.Id });
            }

            // Find seller
            var seller = await _userManager.FindByIdAsync(
                product.SellerId);

            if (seller == null)
            {
                TempData["ErrorMessage"] =
                    "The seller could not be found.";

                return RedirectToAction(
                    "Details",
                    "Marketplace",
                    new { id = product.Id });
            }

            // ========================================================
            // COMMISSION CALCULATION
            // ========================================================

            var commissionAmount = Math.Round(
                product.Price *
                (CommissionPercentage / 100m),
                2);

            var sellerAmount = Math.Round(
                product.Price - commissionAmount,
                2);

            // ========================================================
            // CREATE ORDER
            // ========================================================

            var order = new Order
            {
                ProductId = product.Id,

                BuyerId = buyer.Id,

                SellerId = seller.Id,

                ProductPrice = product.Price,

                CommissionPercentage =
                    CommissionPercentage,

                CommissionAmount =
                    commissionAmount,

                SellerAmount =
                    sellerAmount,

                TotalAmount =
                    product.Price,

                PaymentStatus = "Pending",

                OrderStatus = "Pending",

                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // Information for checkout page
            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;
            ViewBag.Price = product.Price;

            return View(order);
        }
        // ============================================================
        // PLACE ORDER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            int orderId,
            string FullName,
            string PhoneNumber,
            string Country,
            string City,
            string DeliveryAddress)
        {
            var buyer = await _userManager.GetUserAsync(User);

            if (buyer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // --------------------------------------------------------
            // FIND THE EXISTING ORDER
            // --------------------------------------------------------

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.BuyerId == buyer.Id);

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "The order could not be found.";

                return RedirectToAction(
                    "Index",
                    "Marketplace"
                );
            }

            // --------------------------------------------------------
            // CHECK ORDER STATUS
            // --------------------------------------------------------

            if (order.OrderStatus == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "This order has already been cancelled.";

                return RedirectToAction(
                    "Index",
                    "Marketplace"
                );
            }

            // --------------------------------------------------------
            // CHECK PAYMENT STATUS
            // --------------------------------------------------------

            if (order.PaymentStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This order has already been processed.";

                return RedirectToAction(
                    "MyOrders",
                    "Order"
                );
            }

            // --------------------------------------------------------
            // SAVE DELIVERY INFORMATION
            // --------------------------------------------------------

            order.FullName = FullName;
            order.PhoneNumber = PhoneNumber;
            order.Country = Country;
            order.City = City;
            order.DeliveryAddress = DeliveryAddress;

            // --------------------------------------------------------
            // SAVE CHANGES
            // --------------------------------------------------------

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // GO TO PAYMENT PAGE
            // --------------------------------------------------------

            return View("Payment", order);
        }

        


        // ============================================================
        // PAYMENT PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var buyer = await _userManager.GetUserAsync(User);

            if (buyer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.BuyerId == buyer.Id);

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "Order could not be found.";

                return RedirectToAction(
                    "Index",
                    "Marketplace");
            }

            return View(order);
        }


        // ============================================================
        // CANCEL ORDER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var buyer = await _userManager.GetUserAsync(User);

            if (buyer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.BuyerId == buyer.Id);

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "Order could not be found.";

                return RedirectToAction(
                    "Index",
                    "Marketplace");
            }

            // Only pending orders can be cancelled
            if (order.PaymentStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This order can no longer be cancelled.";

                return RedirectToAction(
                    "Index",
                    "Marketplace");
            }

            order.OrderStatus = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Order cancelled successfully.";

            return RedirectToAction(
                "Details",
                "Marketplace",
                new { id = order.ProductId });
        }


        // ============================================================
        // MY ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var buyer = await _userManager.GetUserAsync(User);

            if (buyer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.BuyerId == buyer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }
    }
}