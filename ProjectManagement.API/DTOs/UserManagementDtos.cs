using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.API.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }

        public bool IsActive { get; set; } = true;

        public int? PortfolioId { get; set; }
        public int? ProgramId { get; set; }
        public int? ProjectId { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Password { get; set; } // Optional: only update if provided

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }

        public bool IsActive { get; set; } = true;

        public int? PortfolioId { get; set; }
        public int? ProgramId { get; set; }
        public int? ProjectId { get; set; }
    }

    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public List<UserPortfolioDto> Portfolios { get; set; } = new List<UserPortfolioDto>();
        public List<UserProgramDto> Programs { get; set; } = new List<UserProgramDto>();
        public List<UserProjectDto> Projects { get; set; } = new List<UserProjectDto>();
    }

    public class UserPortfolioDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int ProgramsCount { get; set; }
        public int ProjectsCount { get; set; }
        public decimal Progress { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserProgramDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int ProjectsCount { get; set; }
        public decimal Progress { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TasksCount { get; set; }
        public decimal Progress { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
