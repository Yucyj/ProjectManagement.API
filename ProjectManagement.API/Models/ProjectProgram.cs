using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ProjectProgram
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Budget { get; set; }

        public int Status { get; set; } // 1 = Active/In Progress, 2 = Completed, 3 = Pending, 4 = Rejected

        public int ProgressPercentage { get; set; } = 0; // لعرض نسبة الـ 70% في الـ Figma

        [MaxLength(150)]
        public string SponsorName { get; set; } = string.Empty;

        [Required]
        public string ManagerId { get; set; } = string.Empty;

        [ForeignKey("ManagerId")]
        public ApplicationUser? Manager { get; set; }

        // ربط البرنامج بالمحفظة الأب (Portfolio)
        public int PortfolioId { get; set; }

        [ForeignKey("PortfolioId")]
        public Portfolio? Portfolio { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // الاحتفاظ بروابط المستندات المرفوعة بشكل مبسط (كـ URLs مفصولة بفاصلة)
        public string? AttachedDocumentUrls { get; set; }

        // علاقات التنقل والأبناء للـ API Metrics
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
