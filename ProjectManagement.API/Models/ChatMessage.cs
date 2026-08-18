using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser? Sender { get; set; }

        // Null because this is now a company-wide message.
        // There is no single receiver.
        public string? ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser? Receiver { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual ICollection<MessageReaction> Reactions { get; set; }
            = new List<MessageReaction>();

        public int? ReplyToMessageId { get; set; }

        [ForeignKey(nameof(ReplyToMessageId))]
        public ChatMessage? ReplyToMessage { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }
    }
}