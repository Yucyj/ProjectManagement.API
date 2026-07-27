using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreateRoleDto
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
