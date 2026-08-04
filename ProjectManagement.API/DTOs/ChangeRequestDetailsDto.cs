using System;

namespace ProjectManagement.API.DTOs
{
    public class ChangeRequestDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal ImpactCost { get; set; }
        public int ImpactTimeDays { get; set; }
        public int Status { get; set; } // 1 = Pending, 2 = Approved, 3 = Rejected
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string RequestedById { get; set; } = string.Empty;
        public string RequestedByUserName { get; set; } = string.Empty;
        public string? ApprovedById { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? AttachedFiles { get; set; }
    }
}
