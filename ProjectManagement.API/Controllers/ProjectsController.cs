using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int MapStatusStringToInt(string? statusStr)
        {
            if (string.IsNullOrEmpty(statusStr)) return 1;
            switch (statusStr.ToLower())
            {
                case "active":
                case "نشط":
                    return 1;
                case "completed":
                case "مكتمل":
                    return 2;
                case "pending":
                case "onhold":
                case "قيد الانتظار":
                    return 3;
                case "rejected":
                case "refusing":
                case "مرفوض":
                    return 4;
                default:
                    return 1;
            }
        }

        // 1. Get projects list with filters
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ProjectDetailsDto>>> GetProjects(
            [FromQuery] int? portfolioId,
            [FromQuery] int? programId,
            [FromQuery] string? keyword,
            [FromQuery] string? status)
        {
            var query = _context.Projects
                .Include(p => p.Portfolio)
                .Include(p => p.Program)
                .Include(p => p.Tasks)
                .Include(p => p.ProjectMembers)
                .AsQueryable();

            if (portfolioId.HasValue)
                query = query.Where(p => p.PortfolioId == portfolioId.Value);

            if (programId.HasValue)
                query = query.Where(p => p.ProgramId == programId.Value);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));

            if (!string.IsNullOrEmpty(status))
            {
                int statusVal = MapStatusStringToInt(status);
                query = query.Where(p => p.Status == statusVal);
            }

            var projects = await query.Select(p => new ProjectDetailsDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Budget = p.Budget,
                Status = p.Status == 1 ? "Active" :
                         p.Status == 2 ? "Completed" :
                         p.Status == 3 ? "OnHold" : "Rejected",
                Priority = p.Priority,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                ManagerName = !string.IsNullOrEmpty(p.ManagerName) ? p.ManagerName : (p.Manager != null ? p.Manager.UserName : "Not Assigned"),
                PortfolioName = p.Portfolio != null ? p.Portfolio.Name : "N/A",
                PortfolioId = p.PortfolioId,
                ProgramName = p.Program != null ? p.Program.Name : null,
                ProgramId = p.ProgramId,
                AttachedFiles = p.AttachedFiles,
                TasksCount = p.Tasks.Count,
                MembersCount = p.ProjectMembers.Count
            }).ToListAsync();

            return Ok(projects);
        }

        // 2. Get single project details
        [HttpGet("details/{id}")]
        public async Task<ActionResult<ProjectDetailsDto>> GetProject(int id)
        {
            var p = await _context.Projects
                .Include(proj => proj.Portfolio)
                .Include(proj => proj.Program)
                .Include(proj => proj.Tasks)
                .Include(proj => proj.ProjectMembers)
                .FirstOrDefaultAsync(proj => proj.Id == id);

            if (p == null)
                return NotFound();

            var result = new ProjectDetailsDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Budget = p.Budget,
                Status = p.Status == 1 ? "Active" :
                         p.Status == 2 ? "Completed" :
                         p.Status == 3 ? "OnHold" : "Rejected",
                Priority = p.Priority,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                ManagerName = !string.IsNullOrEmpty(p.ManagerName) ? p.ManagerName : (p.Manager != null ? p.Manager.UserName : "Not Assigned"),
                PortfolioName = p.Portfolio != null ? p.Portfolio.Name : "N/A",
                PortfolioId = p.PortfolioId,
                ProgramName = p.Program != null ? p.Program.Name : null,
                ProgramId = p.ProgramId,
                AttachedFiles = p.AttachedFiles,
                TasksCount = p.Tasks.Count,
                MembersCount = p.ProjectMembers.Count
            };

            return Ok(result);
        }

        // 3. Create project
        [HttpPost("create")]
        public async Task<ActionResult<Project>> CreateProject([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                userId = firstUser?.Id ?? "default-user-id";
            }

            // Validate Portfolio exists
            var portfolioExists = await _context.Portfolios.AnyAsync(p => p.Id == dto.PortfolioId);
            if (!portfolioExists)
            {
                return BadRequest(new { message = $"Portfolio with ID {dto.PortfolioId} does not exist. Please create a portfolio first and use its ID." });
            }

            // Treat ProgramId 0 as null, otherwise validate if it exists
            int? resolvedProgramId = (dto.ProgramId.HasValue && dto.ProgramId.Value != 0) ? dto.ProgramId.Value : null;
            if (resolvedProgramId.HasValue)
            {
                var programExists = await _context.Programs.AnyAsync(p => p.Id == resolvedProgramId.Value);
                if (!programExists)
                {
                    return BadRequest(new { message = $"Program with ID {resolvedProgramId.Value} does not exist." });
                }
            }

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Budget = dto.Budget,
                Status = MapStatusStringToInt(dto.Status),
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                ManagerName = dto.ManagerName,
                ManagerId = userId, // Assign current user fallback
                PortfolioId = dto.PortfolioId,
                ProgramId = resolvedProgramId,
                AttachedFiles = dto.AttachedFiles,
                CreatedDate = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        // 4. Update project
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Validate Portfolio exists
            var portfolioExists = await _context.Portfolios.AnyAsync(p => p.Id == dto.PortfolioId);
            if (!portfolioExists)
            {
                return BadRequest(new { message = $"Portfolio with ID {dto.PortfolioId} does not exist." });
            }

            // Treat ProgramId 0 as null, otherwise validate if it exists
            int? resolvedProgramId = (dto.ProgramId.HasValue && dto.ProgramId.Value != 0) ? dto.ProgramId.Value : null;
            if (resolvedProgramId.HasValue)
            {
                var programExists = await _context.Programs.AnyAsync(p => p.Id == resolvedProgramId.Value);
                if (!programExists)
                {
                    return BadRequest(new { message = $"Program with ID {resolvedProgramId.Value} does not exist." });
                }
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Budget = dto.Budget;
            project.Status = MapStatusStringToInt(dto.Status);
            project.Priority = dto.Priority;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.ManagerName = dto.ManagerName;
            project.PortfolioId = dto.PortfolioId;
            project.ProgramId = resolvedProgramId;
            project.AttachedFiles = dto.AttachedFiles;

            _context.Entry(project).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. Delete project
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (project.Tasks.Any())
            {
                return BadRequest("Cannot delete a project that contains active tasks.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project deleted successfully." });
        }

        // 6. Upload files for projects
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("No files uploaded.");
            }

            var uploadedFilesList = new List<object>();
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var originalName = file.FileName;
                    var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(originalName);
                    var filePath = Path.Combine(uploadsFolder, uniqueName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var sizeInMb = (file.Length / (1024.0 * 1024.0)).ToString("F1") + " MB";
                    var ext = Path.GetExtension(originalName).TrimStart('.').ToLower();

                    uploadedFilesList.Add(new
                    {
                        name = originalName,
                        path = "/uploads/" + uniqueName,
                        size = sizeInMb,
                        type = ext
                    });
                }
            }

            return Ok(uploadedFilesList);
        }
    }
}
