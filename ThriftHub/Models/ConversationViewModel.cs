using System;
using System.Collections.Generic;

namespace ThriftHub.Models
{
    public class ConversationViewModel
    {
        // Identifies the conversation
        public string ConversationId { get; set; } = "";

        // Other user's information
        public string UserId { get; set; } = "";

        public string UserName { get; set; } = "";

        public string UserEmail { get; set; } = "";

        public string UserRole { get; set; } = "";

        public string ProfileImageUrl { get; set; } = "";

        public bool IsOnline { get; set; }

        // Last message information
        public DateTime? LastMessageTime { get; set; }

        public string LastMessageText { get; set; } = "";

        // Number of unread messages
        public int UnreadCount { get; set; }

        // Messages inside the conversation
        public List<MessageViewModel> Messages { get; set; }
            = new List<MessageViewModel>();
    }
}