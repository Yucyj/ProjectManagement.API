using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreateSuperAdminDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
