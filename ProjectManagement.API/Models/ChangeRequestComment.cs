using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.API.Models
{
    public class ChangeRequestComment
    {
        public int Id { get; set; }

        public int ChangeRequestId { get; set; }

        [ForeignKey("ChangeRequestId")]
        public ChangeRequest? ChangeRequest { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
