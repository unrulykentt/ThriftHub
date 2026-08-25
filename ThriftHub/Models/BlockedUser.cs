using System;
using System.ComponentModel.DataAnnotations;

namespace ThriftHub.Models
{
    public class BlockedUser
    {
        public int Id { get; set; }

        // ============================================================
        // USER WHO IS BLOCKING
        // ============================================================

        [Required]
        public string BlockerId { get; set; } = string.Empty;


        // ============================================================
        // USER WHO IS BEING BLOCKED
        // ============================================================

        [Required]
        public string BlockedUserId { get; set; } = string.Empty;


        // ============================================================
        // DATE BLOCKED
        // ============================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}