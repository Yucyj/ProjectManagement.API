using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class Portfolio
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Budget { get; set; }

        public int Status { get; set; } // 1 = Active, 2 = Completed, 3 = Pending, 4 = Rejected

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser? Owner { get; set; }

        [MaxLength(150)]
        public string SponsorName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string ManagerName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ProjectProgram> Programs { get; set; } = new List<ProjectProgram>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
