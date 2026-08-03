using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class UpdateMeetingDto
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(50)]
        public string Time { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        public string? Description { get; set; }

        [MaxLength(100)]
        public string Status { get; set; } = "Pending";

        public string? InvitedMembers { get; set; }

        public string? AttachedFiles { get; set; }

        [Required]
        public int ProjectId { get; set; }
    }
}
