using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class VerifyOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
