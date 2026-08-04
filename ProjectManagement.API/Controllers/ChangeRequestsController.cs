using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;

namespace ProjectManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChangeRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChangeRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Get all change requests (optional project filter)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ChangeRequestDetailsDto>>> GetChangeRequests(
            [FromQuery] int? projectId,
            [FromQuery] string? keyword)
        {
            var query = _context.ChangeRequests
                .Include(cr => cr.Project)
                .Include(cr => cr.RequestedBy)
                .Include(cr => cr.ApprovedBy)
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(cr => cr.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(cr => cr.Title.Contains(keyword) || cr.Description.Contains(keyword) || cr.Reason.Contains(keyword));
            }

            var list = await query.ToListAsync();

            var result = list.Select(cr => new ChangeRequestDetailsDto
            {
                Id = cr.Id,
                Title = cr.Title,
                Description = cr.Description,
                Reason = cr.Reason,
                ImpactCost = cr.ImpactCost,
                ImpactTimeDays = cr.ImpactTimeDays,
                Status = cr.Status,
                ProjectId = cr.ProjectId,
                ProjectName = cr.Project != null ? cr.Project.Name : "N/A",
                RequestedById = cr.RequestedById,
                RequestedByUserName = cr.RequestedBy != null ? cr.RequestedBy.UserName ?? "User" : "User",
                ApprovedById = cr.ApprovedById,
                ApprovedByUserName = cr.ApprovedBy != null ? cr.ApprovedBy.UserName : null,
                RequestDate = cr.RequestDate,
                ActionDate = cr.ActionDate,
                AttachedFiles = cr.AttachedFiles
            }).ToList();

            return Ok(result);
        }

        // 2. Get single change request details
        [HttpGet("details/{id}")]
        public async Task<ActionResult<ChangeRequestDetailsDto>> GetChangeRequest(int id)
        {
            var cr = await _context.ChangeRequests
                .Include(c => c.Project)
                .Include(c => c.RequestedBy)
                .Include(c => c.ApprovedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cr == null)
            {
                return NotFound();
            }

            var result = new ChangeRequestDetailsDto
            {
                Id = cr.Id,
                Title = cr.Title,
                Description = cr.Description,
                Reason = cr.Reason,
                ImpactCost = cr.ImpactCost,
                ImpactTimeDays = cr.ImpactTimeDays,
                Status = cr.Status,
                ProjectId = cr.ProjectId,
                ProjectName = cr.Project != null ? cr.Project.Name : "N/A",
                RequestedById = cr.RequestedById,
                RequestedByUserName = cr.RequestedBy != null ? cr.RequestedBy.UserName ?? "User" : "User",
                ApprovedById = cr.ApprovedById,
                ApprovedByUserName = cr.ApprovedBy != null ? cr.ApprovedBy.UserName : null,
                RequestDate = cr.RequestDate,
                ActionDate = cr.ActionDate,
                AttachedFiles = cr.AttachedFiles
            };

            return Ok(result);
        }

        // 3. Create change request
        [HttpPost("create")]
        public async Task<ActionResult<ChangeRequest>> CreateChangeRequest([FromBody] CreateChangeRequestDto dto)
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

            // Validate Project
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist." });
            }

            var cr = new ChangeRequest
            {
                Title = dto.Title,
                Description = dto.Description,
                Reason = dto.Reason,
                ImpactCost = dto.ImpactCost,
                ImpactTimeDays = dto.ImpactTimeDays,
                Status = 1, // 1 = Pending
                ProjectId = dto.ProjectId,
                RequestedById = userId,
                RequestDate = DateTime.UtcNow,
                AttachedFiles = dto.AttachedFiles
            };

            _context.ChangeRequests.Add(cr);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChangeRequest), new { id = cr.Id }, cr);
        }

        // 4. Update change request
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateChangeRequest(int id, [FromBody] UpdateChangeRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cr = await _context.ChangeRequests.FindAsync(id);
            if (cr == null)
            {
                return NotFound();
            }

            // Validate Project
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist." });
            }

            cr.Title = dto.Title;
            cr.Description = dto.Description;
            cr.Reason = dto.Reason;
            cr.ImpactCost = dto.ImpactCost;
            cr.ImpactTimeDays = dto.ImpactTimeDays;
            cr.ProjectId = dto.ProjectId;
            cr.AttachedFiles = dto.AttachedFiles;

            _context.Entry(cr).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. Approve change request
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveChangeRequest(int id, [FromQuery] string? approvedById)
        {
            var cr = await _context.ChangeRequests.FindAsync(id);
            if (cr == null)
            {
                return NotFound();
            }

            var userId = approvedById;
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    var firstUser = await _context.Users.FirstOrDefaultAsync();
                    userId = firstUser?.Id ?? "default-user-id";
                }
            }

            cr.Status = 2; // 2 = Approved
            cr.ApprovedById = userId;
            cr.ActionDate = DateTime.UtcNow;

            _context.Entry(cr).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Change request approved successfully." });
        }

        // 6. Reject change request
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectChangeRequest(int id, [FromQuery] string? approvedById)
        {
            var cr = await _context.ChangeRequests.FindAsync(id);
            if (cr == null)
            {
                return NotFound();
            }

            var userId = approvedById;
            if (string.IsNullOrEmpty(userId))
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    var firstUser = await _context.Users.FirstOrDefaultAsync();
                    userId = firstUser?.Id ?? "default-user-id";
                }
            }

            cr.Status = 3; // 3 = Rejected
            cr.ApprovedById = userId;
            cr.ActionDate = DateTime.UtcNow;

            _context.Entry(cr).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Change request rejected successfully." });
        }

        // 7. Delete change request
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteChangeRequest(int id)
        {
            var cr = await _context.ChangeRequests.FindAsync(id);
            if (cr == null)
            {
                return NotFound();
            }

            _context.ChangeRequests.Remove(cr);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Change request deleted successfully." });
        }

        // 8. Upload files for change requests
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

                    uploadedFilesList.Add(new
                    {
                        originalName = originalName,
                        uniqueName = uniqueName,
                        filePath = $"/uploads/{uniqueName}"
                    });
                }
            }

            return Ok(uploadedFilesList);
        }
    }
}
