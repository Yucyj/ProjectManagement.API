using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class UpdateProgramDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public int Status { get; set; }
        public int ProgressPercentage { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string ManagerId { get; set; } = string.Empty;
        public List<string>? AttachedUrls { get; set; }
    }
}
