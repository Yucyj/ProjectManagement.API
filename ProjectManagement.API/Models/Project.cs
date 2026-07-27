using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Budget { get; set; }

        public int Status { get; set; }

        public int Priority { get; set; } // 1 = Low, 2 = Medium, 3 = High

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        public string ManagerId { get; set; } = string.Empty;

        [ForeignKey("ManagerId")]
        public ApplicationUser? Manager { get; set; }

        public int PortfolioId { get; set; }

        [ForeignKey("PortfolioId")]
        public Portfolio? Portfolio { get; set; }

        public int? ProgramId { get; set; }

        [ForeignKey("ProgramId")]
        public ProjectProgram? Program { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
        public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
    }
}
