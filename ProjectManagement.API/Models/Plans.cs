using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.Models
{
    public class Plan
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        public string Version { get; set; } = "v1.0";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public List<PlanMilestone> Milestones { get; set; } = new();
        public List<PlanDeliverable> Deliverables { get; set; } = new();
    }

    public class PlanMilestone
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
    }

    public class PlanDeliverable
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ExpectedCompletionDate { get; set; }
    }
}