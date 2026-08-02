using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class UpdateProjectDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Range(1, double.MaxValue)]
        public decimal Budget { get; set; }
        
        public string Status { get; set; } = "Active";
        
        [Range(1, 3)]
        public int Priority { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        [MaxLength(150)]
        public string ManagerName { get; set; } = string.Empty;
        
        public int PortfolioId { get; set; }
        public int? ProgramId { get; set; }
        public string? AttachedFiles { get; set; }
    }
}
