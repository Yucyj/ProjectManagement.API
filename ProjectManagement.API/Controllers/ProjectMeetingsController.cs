using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ProjectMeetingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectMeetingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Get all meetings with filters
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<MeetingDetailsDto>>> GetMeetings(
            [FromQuery] int? projectId,
            [FromQuery] string? keyword)
        {
            var query = _context.Meetings
                .Include(m => m.Project)
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(m => m.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.Title.Contains(keyword) || (m.Description != null && m.Description.Contains(keyword)));
            }

            var meetings = await query.ToListAsync();

            var result = meetings.Select(m => new MeetingDetailsDto
            {
                Id = m.Id,
                Title = m.Title,
                Date = m.Date,
                Time = m.Time,
                MeetingLink = m.MeetingLink,
                Description = m.Description,
                Status = m.Status,
                InvitedMembers = m.InvitedMembers,
                AttachedFiles = m.AttachedFiles,
                ProjectId = m.ProjectId,
                ProjectName = m.Project != null ? m.Project.Name : "N/A",
                CreatedDate = m.CreatedDate
            }).ToList();

            return Ok(result);
        }

        // 2. Get single meeting details
        [HttpGet("details/{id}")]
        public async Task<ActionResult<MeetingDetailsDto>> GetMeeting(int id)
        {
            var m = await _context.Meetings
                .Include(meeting => meeting.Project)
                .FirstOrDefaultAsync(meeting => meeting.Id == id);

            if (m == null)
            {
                return NotFound();
            }

            var result = new MeetingDetailsDto
            {
                Id = m.Id,
                Title = m.Title,
                Date = m.Date,
                Time = m.Time,
                MeetingLink = m.MeetingLink,
                Description = m.Description,
                Status = m.Status,
                InvitedMembers = m.InvitedMembers,
                AttachedFiles = m.AttachedFiles,
                ProjectId = m.ProjectId,
                ProjectName = m.Project != null ? m.Project.Name : "N/A",
                CreatedDate = m.CreatedDate
            };

            return Ok(result);
        }

        // 3. Create meeting
        [HttpPost("create")]
        public async Task<ActionResult<ProjectMeeting>> CreateMeeting([FromBody] CreateMeetingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate Project exists
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist. Please create a project first." });
            }

            var meeting = new ProjectMeeting
            {
                Title = dto.Title,
                Date = dto.Date,
                Time = dto.Time,
                MeetingLink = dto.MeetingLink,
                Description = dto.Description,
                Status = dto.Status,
                InvitedMembers = dto.InvitedMembers,
                AttachedFiles = dto.AttachedFiles,
                ProjectId = dto.ProjectId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, meeting);
        }

        // 4. Update meeting
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, [FromBody] UpdateMeetingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }

            // Validate Project exists
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = $"Project with ID {dto.ProjectId} does not exist." });
            }

            meeting.Title = dto.Title;
            meeting.Date = dto.Date;
            meeting.Time = dto.Time;
            meeting.MeetingLink = dto.MeetingLink;
            meeting.Description = dto.Description;
            meeting.Status = dto.Status;
            meeting.InvitedMembers = dto.InvitedMembers;
            meeting.AttachedFiles = dto.AttachedFiles;
            meeting.ProjectId = dto.ProjectId;

            _context.Entry(meeting).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. Delete meeting
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Meeting deleted successfully." });
        }

        // 6. Upload files for meetings
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
