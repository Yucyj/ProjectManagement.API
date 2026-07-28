using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreatePortfolioDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string SponsorName { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;
    }
}