using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Status { get; set; } // 1 = To Do, 2 = In Progress, 3 = In Review, 4 = Done

        public int Priority { get; set; } // 1 = Low, 2 = Medium, 3 = High

        public DateTime? DueDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public string? AssigneeId { get; set; }

        [ForeignKey("AssigneeId")]
        public ApplicationUser? Assignee { get; set; }
    }
}
