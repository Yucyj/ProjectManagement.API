using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ProjectMeeting
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [MaxLength(50)]
        public string Time { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        public string? Description { get; set; }

        [MaxLength(100)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Completed, Cancelled

        // Comma-separated list of invited member names
        public string? InvitedMembers { get; set; }

        // Attached files JSON list
        public string? AttachedFiles { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
