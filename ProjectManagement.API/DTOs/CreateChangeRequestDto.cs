using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreateChangeRequestDto
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public decimal ImpactCost { get; set; }

        [Required]
        public int ImpactTimeDays { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public string? AttachedFiles { get; set; }
    }
}
