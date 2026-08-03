using System;

namespace ProjectManagement.API.DTOs
{
    public class MeetingDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string? MeetingLink { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public string? InvitedMembers { get; set; }
        public string? AttachedFiles { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
