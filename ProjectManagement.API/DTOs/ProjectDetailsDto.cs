using System;

namespace ProjectManagement.API.DTOs
{
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public string Status { get; set; } = "Active";
        public int Priority { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string? PortfolioName { get; set; }
        public int PortfolioId { get; set; }
        public string? ProgramName { get; set; }
        public int? ProgramId { get; set; }
        public string? AttachedFiles { get; set; }
        
        // Count metrics for front-end rendering
        public int TasksCount { get; set; }
        public int MembersCount { get; set; }
    }
}
