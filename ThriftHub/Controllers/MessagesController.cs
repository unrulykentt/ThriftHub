using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Hubs;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly NotificationService _notificationService;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            IHubContext<ChatHub> hubContext,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var currentUserId =
                currentUser.Id;

            var allMessages =
                await _context.Messages
                    .Where(m =>
                        m.SenderId == currentUserId ||
                        m.RecipientId == currentUserId)
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();

            var otherUserIds =
                allMessages
                    .Select(m =>
                        m.SenderId == currentUserId
                            ? m.RecipientId
                            : m.SenderId)
                    .Where(id =>
                        !string.IsNullOrWhiteSpace(id) &&
                        id != currentUserId)
                    .Distinct()
                    .ToList();

            var conversations =
                new List<ConversationViewModel>();

            foreach (var otherUserId in otherUserIds)
            {
                var otherUser =
                    await _userManager
                        .FindByIdAsync(
                            otherUserId);

                if (otherUser == null)
                {
                    continue;
                }

                var conversationMessages =
                    allMessages
                        .Where(m =>
                            (m.SenderId == currentUserId &&
                             m.RecipientId == otherUserId)
                            ||
                            (m.SenderId == otherUserId &&
                             m.RecipientId == currentUserId))
                        .OrderBy(m => m.SentAt)
                        .ToList();

                if (!conversationMessages.Any())
                {
                    continue;
                }

                var unreadCount =
                    conversationMessages.Count(
                        m =>
                            m.RecipientId ==
                                currentUserId &&
                            !m.IsRead);

                var conversation =
                    new ConversationViewModel
                    {
                        UserId =
                            otherUser.Id,

                        UserEmail =
                            otherUser.Email ??
                            string.Empty,

                        UserName =
                            !string.IsNullOrWhiteSpace(
                                otherUser.UserName)
                                ? otherUser.UserName
                                : otherUser.Email ??
                                  "User",

                        UserRole = "Buyer",

                        ProfileImageUrl =
                            !string.IsNullOrWhiteSpace(
                                otherUser.ProfileImageUrl)
                                ? otherUser.ProfileImageUrl
                                : "/images/default-avatar.png",

                        IsOnline =
                            otherUser.IsOnline,

                        Messages =
                            conversationMessages
                                .Select(m =>
                                    new MessageViewModel
                                    {
                                        Id = m.Id,

                                        SenderId =
                                            m.SenderId,

                                        RecipientId =
                                            m.RecipientId,

                                        Content =
                                            m.Content,

                                        MessageType =
                                            m.MessageType,

                                        FileUrl =
                                            m.FileUrl,

                                        FileName =
                                            m.FileName,

                                        FileSize =
                                            m.FileSize,

                                        SentAt =
                                            m.SentAt,

                                        IsRead =
                                            m.IsRead
                                    })
                                .ToList(),

                        UnreadCount =
                            unreadCount
                    };

                conversations.Add(
                    conversation);
            }

            conversations =
                conversations
                    .OrderByDescending(
                        c =>
                            c.Messages
                                .OrderByDescending(
                                    m => m.SentAt)
                                .Select(
                                    m => (DateTime?)
                                        m.SentAt)
                                .FirstOrDefault())
                    .ToList();

            return View(
                conversations);
        }

        // ============================================================
        // CHAT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Chat(
            string? userId,
            string? sellerId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = sellerId;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(
                    "User ID is required.");
            }

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var currentUserId =
                currentUser.Id;

            if (currentUserId == userId)
            {
                return BadRequest(
                    "You cannot open a conversation with yourself.");
            }

            var otherUser =
                await _userManager
                    .FindByIdAsync(userId);

            if (otherUser == null)
            {
                return NotFound(
                    "User not found.");
            }

            var conversationMessages =
                await _context.Messages
                    .Where(m =>
                        (m.SenderId == currentUserId &&
                         m.RecipientId == userId)
                        ||
                        (m.SenderId == userId &&
                         m.RecipientId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

            var unreadMessages =
                conversationMessages
                    .Where(m =>
                        m.RecipientId ==
                            currentUserId &&
                        !m.IsRead)
                    .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();

                await _hubContext.Clients
                    .User(userId)
                    .SendAsync(
                        "MessagesRead",
                        currentUserId);
            }

            var messageViewModels =
                conversationMessages
                    .Select(m =>
                        new MessageViewModel
                        {
                            Id = m.Id,

                            SenderId =
                                m.SenderId,

                            RecipientId =
                                m.RecipientId,

                            Content =
                                m.Content,

                            MessageType =
                                m.MessageType,

                            FileUrl =
                                m.FileUrl,

                            FileName =
                                m.FileName,

                            FileSize =
                                m.FileSize,

                            SentAt =
                                m.SentAt,

                            IsRead =
                                m.IsRead
                        })
                    .ToList();

            var model =
                new ConversationViewModel
                {
                    UserId =
                        otherUser.Id,

                    UserEmail =
                        otherUser.Email ??
                        string.Empty,

                    UserName =
                        !string.IsNullOrWhiteSpace(
                            otherUser.UserName)
                            ? otherUser.UserName
                            : otherUser.Email ??
                              "User",

                    UserRole = "Buyer",

                    ProfileImageUrl =
                        !string.IsNullOrWhiteSpace(
                            otherUser.ProfileImageUrl)
                            ? otherUser.ProfileImageUrl
                            : "/images/default-avatar.png",

                    IsOnline =
                        otherUser.IsOnline,

                    Messages =
                        messageViewModels,

                    UnreadCount = 0
                };

            return View(model);
        }

        // ============================================================
        // SEND TEXT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            string recipientId,
            string content)
        {
            if (string.IsNullOrWhiteSpace(
                recipientId))
            {
                return BadRequest(
                    "Recipient ID is required.");
            }

            if (string.IsNullOrWhiteSpace(
                content))
            {
                return BadRequest(
                    "Message cannot be empty.");
            }

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var recipient =
                await _userManager
                    .FindByIdAsync(
                        recipientId);

            if (recipient == null)
            {
                return NotFound(
                    "Recipient not found.");
            }

            if (currentUser.Id ==
                recipient.Id)
            {
                return BadRequest(
                    "You cannot send a message to yourself.");
            }

            var message =
                new Message
                {
                    SenderId =
                        currentUser.Id,

                    RecipientId =
                        recipient.Id,

                    Content =
                        content.Trim(),

                    MessageType =
                        "text",

                    FileUrl = null,

                    FileName = null,

                    FileSize = 0,

                    SentAt =
                        DateTime.UtcNow,

                    IsRead = false
                };

            _context.Messages.Add(
                message);

            await _context.SaveChangesAsync();

            var senderName =
                currentUser.UserName ??
                currentUser.Email ??
                "User";

            var payload =
                new
                {
                    id = message.Id,

                    senderId =
                        currentUser.Id,

                    senderName =
                        senderName,

                    recipientId =
                        recipient.Id,

                    content =
                        message.Content,

                    messageType =
                        message.MessageType,

                    fileUrl = "",

                    fileName = "",

                    sentAt =
                        message.SentAt,

                    isRead =
                        message.IsRead
                };

            await _hubContext.Clients
                .User(recipient.Id)
                .SendAsync(
                    "NewMessage",
                    payload);

            await _hubContext.Clients
                .User(currentUser.Id)
                .SendAsync(
                    "MessageSent",
                    payload);

            // ========================================================
            // CREATE NOTIFICATION
            // ========================================================

            var notificationMessage =
                $"{senderName} sent you a new message.";

            var notificationLink =
                $"/Messages/Chat?userId={currentUser.Id}";

            await _notificationService.CreateAsync(
                recipient.Id,
                notificationMessage,
                notificationLink);

            // ========================================================
            // SEND REAL-TIME NOTIFICATION
            // ========================================================

            await _hubContext.Clients
                .User(recipient.Id)
                .SendAsync(
                    "NewNotification",
                    new
                    {
                        message =
                            notificationMessage,

                        link =
                            notificationLink,

                        createdAt =
                            DateTime.UtcNow
                    });

            return Ok(
                new
                {
                    success = true,

                    id = message.Id
                });
        }

        // ============================================================
        // SEND FILE / IMAGE / AUDIO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFile(
            string recipientId,
            IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(
                recipientId))
            {
                return BadRequest(
                    "Recipient ID is required.");
            }

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var recipient =
                await _userManager
                    .FindByIdAsync(
                        recipientId);

            if (recipient == null)
            {
                return NotFound(
                    "Recipient not found.");
            }

            if (currentUser.Id ==
                recipient.Id)
            {
                return BadRequest(
                    "You cannot send a file to yourself.");
            }

            if (file == null ||
                file.Length == 0)
            {
                return BadRequest(
                    "Please select a file.");
            }

            if (file.Length >
                20 * 1024 * 1024)
            {
                return BadRequest(
                    "File size must not exceed 20 MB.");
            }

            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "messages");

            Directory.CreateDirectory(
                uploadsFolder);

            var extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();

            var uniqueFileName =
                $"{Guid.NewGuid()}{extension}";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
            {
                await file.CopyToAsync(
                    stream);
            }

            var fileUrl =
                $"/uploads/messages/{uniqueFileName}";

            var fileName =
                Path.GetFileName(
                    file.FileName);

            string messageType =
                "file";

            var contentType =
                file.ContentType ?? string.Empty;

            if (contentType
                .StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase))
            {
                messageType =
                    "image";
            }
            else if (
                contentType
                    .StartsWith(
                        "video/",
                        StringComparison.OrdinalIgnoreCase))
            {
                messageType =
                    "video";
            }
            else if (
                contentType
                    .StartsWith(
                        "audio/",
                        StringComparison.OrdinalIgnoreCase)
                ||
                IsAudioExtension(extension))
            {
                messageType =
                    "audio";
            }

            var message =
                new Message
                {
                    SenderId =
                        currentUser.Id,

                    RecipientId =
                        recipient.Id,

                    Content =
                        string.Empty,

                    MessageType =
                        messageType,

                    FileUrl =
                        fileUrl,

                    FileName =
                        fileName,

                    FileSize =
                        file.Length,

                    SentAt =
                        DateTime.UtcNow,

                    IsRead =
                        false
                };

            _context.Messages.Add(
                message);

            await _context.SaveChangesAsync();

            var senderName =
                currentUser.UserName ??
                currentUser.Email ??
                "User";

            var payload =
                new
                {
                    id = message.Id,

                    senderId =
                        currentUser.Id,

                    senderName =
                        senderName,

                    recipientId =
                        recipient.Id,

                    content = "",

                    messageType =
                        message.MessageType,

                    fileUrl =
                        message.FileUrl,

                    fileName =
                        message.FileName,

                    sentAt =
                        message.SentAt,

                    isRead =
                        false
                };

            await _hubContext.Clients
                .User(recipient.Id)
                .SendAsync(
                    "NewMessage",
                    payload);

            await _hubContext.Clients
                .User(currentUser.Id)
                .SendAsync(
                    "MessageSent",
                    payload);

            // ========================================================
            // CREATE NOTIFICATION
            // ========================================================

            string notificationMessage;

            if (messageType == "image")
            {
                notificationMessage =
                    $"{senderName} sent you an image.";
            }
            else if (messageType == "audio")
            {
                notificationMessage =
                    $"{senderName} sent you an audio message.";
            }
            else if (messageType == "video")
            {
                notificationMessage =
                    $"{senderName} sent you a video.";
            }
            else
            {
                notificationMessage =
                    $"{senderName} sent you a file.";
            }

            var notificationLink =
                $"/Messages/Chat?userId={currentUser.Id}";

            await _notificationService.CreateAsync(
                recipient.Id,
                notificationMessage,
                notificationLink);

            // ========================================================
            // SEND REAL-TIME NOTIFICATION
            // ========================================================

            await _hubContext.Clients
                .User(recipient.Id)
                .SendAsync(
                    "NewNotification",
                    new
                    {
                        message =
                            notificationMessage,

                        link =
                            notificationLink,

                        createdAt =
                            DateTime.UtcNow
                    });

            return Ok(
                new
                {
                    success = true,

                    id = message.Id,

                    senderId =
                        currentUser.Id,

                    recipientId =
                        recipient.Id,

                    content = "",

                    messageType =
                        message.MessageType,

                    fileUrl =
                        message.FileUrl,

                    fileName =
                        message.FileName,

                    sentAt =
                        message.SentAt,

                    isRead =
                        false
                });
        }


        private static bool IsAudioExtension(
            string extension)
        {
            return extension is ".webm"
                or ".m4a"
                or ".mp4"
                or ".mp3"
                or ".wav"
                or ".ogg"
                or ".aac";
        }

        // ============================================================
        // MARK READ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return BadRequest(
                    "User ID is required.");
            }

            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var messages =
                await _context.Messages
                    .Where(m =>
                        m.SenderId == userId &&
                        m.RecipientId ==
                            currentUser.Id &&
                        !m.IsRead)
                    .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            if (messages.Any())
            {
                await _context.SaveChangesAsync();

                await _hubContext.Clients
                    .User(userId)
                    .SendAsync(
                        "MessagesRead",
                        currentUser.Id);
            }

            return Ok(
                new
                {
                    success = true
                });
        }

        // ============================================================
        // UNREAD COUNT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult>
            UnreadCount()
        {
            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Json(
                    new
                    {
                        count = 0
                    });
            }

            var count =
                await _context.Messages
                    .CountAsync(
                        m =>
                            m.RecipientId ==
                                currentUser.Id &&
                            !m.IsRead);

            return Json(
                new
                {
                    count
                });
        }

        // ============================================================
        // DELETE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var currentUser =
                await _userManager
                    .GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            var message =
                await _context.Messages
                    .FirstOrDefaultAsync(
                        m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            if (message.SenderId !=
                currentUser.Id)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(
                message.FileUrl))
            {
                try
                {
                    var relativePath =
                        message.FileUrl
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar);

                    var fullPath =
                        Path.Combine(
                            _environment.WebRootPath,
                            relativePath);

                    if (System.IO.File.Exists(
                        fullPath))
                    {
                        System.IO.File.Delete(
                            fullPath);
                    }
                }
                catch
                {
                }
            }

            _context.Messages.Remove(
                message);

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}