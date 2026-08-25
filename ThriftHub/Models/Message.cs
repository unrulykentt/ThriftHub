using System;
using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string RecipientId { get; set; } = string.Empty;

        public string? Content { get; set; }

        // text, image, video, file, audio
        public string MessageType { get; set; } = "text";

        // Attachment information
        public string? FileUrl { get; set; }

        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}