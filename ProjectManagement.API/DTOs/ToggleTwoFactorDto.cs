namespace ProjectManagement.API.DTOs
{
    public class ToggleTwoFactorDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool Enable { get; set; }
    }
}