using Microsoft.AspNetCore.Identity;

namespace ProjectManagement.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? ProfilePhoto { get; set; }
        public string? BackgroundPhoto { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public string? CompanyAr { get; set; }
        public string? CompanyEn { get; set; }
        public string? AboutAr { get; set; }
        public string? AboutEn { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public ICollection<ChangeRequest> RequestedChangeRequests { get; set; } = new List<ChangeRequest>();
        public ICollection<ChangeRequest> ApprovedChangeRequests { get; set; } = new List<ChangeRequest>();
    }
}
