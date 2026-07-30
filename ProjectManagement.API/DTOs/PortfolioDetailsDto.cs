using System;

namespace ProjectManagement.API.DTOs
{
    public class PortfolioDetailsDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Budget { get; set; }
        public string Category { get; set; } = string.Empty;

        public int Status { get; set; }

        public string SponsorName { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? OwnerName { get; set; }

        public int ProjectsCount { get; set; }

        public int ProgramsCount { get; set; }

        // Frontend compatibility mappings
        public string nameAr => Name;
        public string nameEn => Name;
        public string? descriptionAr => Description;
        public string? descriptionEn => Description;
        public string status => Status switch
        {
            1 => "Active",
            2 => "Completed",
            3 => "OnHold",
            4 => "Rejected",
            _ => "Active"
        };
    }
}