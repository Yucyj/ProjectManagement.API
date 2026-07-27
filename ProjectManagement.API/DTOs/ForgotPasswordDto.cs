using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
