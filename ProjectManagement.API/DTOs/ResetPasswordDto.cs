using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class ResetPasswordDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
