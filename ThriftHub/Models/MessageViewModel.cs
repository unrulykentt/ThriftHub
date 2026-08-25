using System;

namespace ThriftHub.Models
{
    public class MessageViewModel
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string RecipientId { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string MessageType { get; set; } = "text";

        public string? FileUrl { get; set; }

        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; }

        public bool IsMine { get; set; }

        // Compatibility properties used by the chat page
        public string? AttachmentUrl => FileUrl;

        public string? AttachmentName => FileName;

        public string AttachmentType => MessageType;
    }
}