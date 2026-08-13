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
    }
}