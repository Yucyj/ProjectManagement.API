using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class UpdatePortfolioDto
    {
        public string? Name { get; set; }
        public string? NameAr { get; set; }

        public string? Description { get; set; }
        public string? DescriptionAr { get; set; }

        [Range(1, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? Status { get; set; } // e.g. "Active" or "OnHold"

        public string SponsorName { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        public string? AttachedFiles { get; set; }
    }
}