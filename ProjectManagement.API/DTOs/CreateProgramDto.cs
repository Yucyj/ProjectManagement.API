using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreateProgramDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public int Status { get; set; } = 3; // افتراضياً Pending بناءً على الـ Figma
        public int PortfolioId { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string ManagerId { get; set; } = string.Empty;
        public List<string>? AttachedUrls { get; set; } // استقبال روابط الملفات المرفوعة من الفرونت إند
    }
}
