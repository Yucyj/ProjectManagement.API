using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreatePortfolioDto
    {
        public string? Name { get; set; }
        public string? NameAr { get; set; }

        public string? Description { get; set; }
        public string? DescriptionAr { get; set; }

        [Range(1, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public string? Status { get; set; } // string e.g. "Active", "OnHold"

        [MaxLength(150)]
        public string SponsorName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string ManagerName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string OwnerName { get; set; } = string.Empty;
    }
}