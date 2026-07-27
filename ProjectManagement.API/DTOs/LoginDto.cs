using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class LoginDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
