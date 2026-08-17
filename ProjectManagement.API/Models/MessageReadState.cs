using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class MessageReadState
    {
        [Key]
        public int Id { get; set; }

        public int MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public ChatMessage? Message { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}