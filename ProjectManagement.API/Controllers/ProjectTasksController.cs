using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int MapStatusStringToInt(string? statusStr)
        {
            if (string.IsNullOrEmpty(statusStr)) return 1;
            switch (statusStr.ToLower().Replace(" ", "").Replace("-", ""))
            {
                case "todo":
                    return 1;
                case "inprogress":
                    return 2;
                case "inreview":
                    return 3;
                case "done":
                    return 4;
                default:
                    return 1;
            }
        }

        private string MapStatusIntToString(int status)
        {
            switch (status)
            {
                case 1: return "To Do";
                case 2: return "In Progress";
                case 3: return "In Review";
                case 4: return "Done";
                default: return "To Do";
            }
        }

        // 1. Get tasks list with filters
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<TaskDetailsDto>>> GetTasks(
            [FromQuery] int? projectId,
            [FromQuery] string? status,
            [FromQuery] string? keyword)
        {
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                int statusVal = MapStatusStringToInt(status);
                query = query.Where(t => t.Status == statusVal);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(t => t.Title.Contains(keyword) || (t.Description != null && t.Description.Contains(keyword)));
            }

            var taskList = await query.ToListAsync();

            var result = taskList.Select(t => new TaskDetailsDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = MapStatusIntToString(t.Status),
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedDate = t.CreatedDate,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "N/A",
                AssigneeName = !string.IsNullOrEmpty(t.AssigneeName) ? t.AssigneeName : (t.Assignee != null ? t.Assignee.UserName : "Not Assigned")
            }).ToList();

            return Ok(result);
        }

        // 2. Get single task details
        [HttpGet("details/{id}")]
        public async Task<ActionResult<TaskDetailsDto>> GetTask(int id)
        {
            var t = await _context.Tasks
                .Include(task => task.Project)
                .FirstOrDefaultAsync(task => task.Id == id);

            if (t == null)
                return NotFound();

            var result = new TaskDetailsDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = MapStatusIntToString(t.Status),
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedDate = t.CreatedDate,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "N/A",
                AssigneeName = !string.IsNullOrEmpty(t.AssigneeName) ? t.AssigneeName : (t.Assignee != null ? t.Assignee.UserName : "Not Assigned")
            };

            return Ok(result);
        }

        // 3. Create task
        [HttpPost("create")]
        public async Task<ActionResult<ProjectTask>> CreateTask([FromBody] CreateTaskDto dto)
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

            // Validate Project exists
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist. Please create a project first." });
            }

            var task = new ProjectTask
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = MapStatusStringToInt(dto.Status),
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                ProjectId = dto.ProjectId,
                AssigneeName = dto.AssigneeName,
                AssigneeId = userId, // Assign current user fallback
                CreatedDate = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        // 4. Update task
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Validate Project exists
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist." });
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = MapStatusStringToInt(dto.Status);
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            task.ProjectId = dto.ProjectId;
            task.AssigneeName = dto.AssigneeName;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. Delete task
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Task deleted successfully." });
        }
    }
}
