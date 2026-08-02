using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string Status { get; set; } = "To Do"; // "To Do", "In Progress", "In Review", "Done"
        
        [Range(1, 3)]
        public int Priority { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High
        
        public DateTime? DueDate { get; set; }
        
        public int ProjectId { get; set; }
        
        [MaxLength(150)]
        public string? AssigneeName { get; set; }
    }
}
