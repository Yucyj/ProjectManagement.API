using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ImpactCost { get; set; }

        public int ImpactTimeDays { get; set; }

        public int Status { get; set; } // 1 = Pending, 2 = Approved, 3 = Rejected

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required]
        public string RequestedById { get; set; } = string.Empty;

        [ForeignKey("RequestedById")]
        public ApplicationUser? RequestedBy { get; set; }

        public string? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? ActionDate { get; set; }
    }
}
