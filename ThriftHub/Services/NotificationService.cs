using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;

namespace ThriftHub.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // CREATE NOTIFICATION
        // ============================================================

        public async Task CreateAsync(
            string userId,
            string message,
            string? link = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }


            var notification = new Notification
            {
                UserId = userId,

                Message = message.Trim(),

                Link = string.IsNullOrWhiteSpace(link)
                    ? null
                    : link,

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };


            _context.Notifications.Add(
                notification);

            await _context.SaveChangesAsync();
        }


        // ============================================================
        // CREATE MESSAGE NOTIFICATION
        // ============================================================

        public async Task CreateMessageNotificationAsync(
            string recipientId,
            string senderName)
        {
            if (string.IsNullOrWhiteSpace(
                recipientId))
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                senderName))
            {
                senderName = "Someone";
            }


            await CreateAsync(
                recipientId,
                $"💬 New message from {senderName}",
                "/Messages/Index");
        }


        // ============================================================
        // CREATE PRODUCT NOTIFICATION
        // ============================================================

        public async Task CreateProductNotificationAsync(
            string userId,
            string message,
            int productId)
        {
            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return;
            }


            await CreateAsync(
                userId,
                message,
                $"/MarketPlace/Details/{productId}");
        }


        // ============================================================
        // GET UNREAD COUNT
        // ============================================================

        public async Task<int> GetUnreadCountAsync(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return 0;
            }


            return await _context.Notifications
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead);
        }
    }
}