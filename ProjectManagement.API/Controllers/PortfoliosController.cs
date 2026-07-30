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
    public class PortfoliosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfoliosController(ApplicationDbContext context)
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioDetailsDto>>> GetPortfolios()
        {
            var portfolios = await _context.Portfolios
                .Include(p => p.Owner)
                .Include(p => p.Programs)
                .Include(p => p.Projects)
                .Select(p => new PortfolioDetailsDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Budget = p.Budget,
                    Category = p.Category,
                    Status = p.Status == 1 ? "Active" :
                             p.Status == 2 ? "Completed" :
                             p.Status == 3 ? "OnHold" : "Rejected",
                    SponsorName = p.SponsorName,
                    ManagerName = p.ManagerName,
                    CreatedDate = p.CreatedDate,
                    OwnerName = !string.IsNullOrEmpty(p.OwnerName) ? p.OwnerName
                                : (p.Owner != null ? p.Owner.UserName : null),
                    AttachedFiles = p.AttachedFiles,
                    ProgramsCount = p.Programs.Count,
                    ProjectsCount = p.Projects.Count
                })
                .ToListAsync();

            return Ok(portfolios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PortfolioDetailsDto>> GetPortfolio(int id)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Owner)
                .Include(p => p.Programs)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (portfolio == null)
                return NotFound();

            var result = new PortfolioDetailsDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                Description = portfolio.Description,
                Budget = portfolio.Budget,
                Category = portfolio.Category,
                Status = portfolio.Status == 1 ? "Active" :
                         portfolio.Status == 2 ? "Completed" :
                         portfolio.Status == 3 ? "OnHold" : "Rejected",
                SponsorName = portfolio.SponsorName,
                ManagerName = portfolio.ManagerName,
                CreatedDate = portfolio.CreatedDate,
                OwnerName = !string.IsNullOrEmpty(portfolio.OwnerName) ? portfolio.OwnerName
                            : portfolio.Owner?.UserName,
                AttachedFiles = portfolio.AttachedFiles,
                ProgramsCount = portfolio.Programs.Count,
                ProjectsCount = portfolio.Projects.Count
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioDto dto)
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

            var portfolio = new Portfolio
            {
                Name = dto.Name ?? dto.NameAr ?? string.Empty,
                Description = dto.Description ?? dto.DescriptionAr,
                Budget = dto.Budget,
                Category = dto.Category ?? string.Empty,
                Status = MapStatusStringToInt(dto.Status),
                SponsorName = dto.SponsorName ?? string.Empty,
                ManagerName = dto.ManagerName ?? string.Empty,
                OwnerName = dto.OwnerName ?? string.Empty,
                AttachedFiles = dto.AttachedFiles,
                OwnerId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();

            var result = new PortfolioDetailsDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                Description = portfolio.Description,
                Budget = portfolio.Budget,
                Category = portfolio.Category,
                Status = portfolio.Status == 1 ? "Active" :
                         portfolio.Status == 2 ? "Completed" :
                         portfolio.Status == 3 ? "OnHold" : "Rejected",
                SponsorName = portfolio.SponsorName,
                ManagerName = portfolio.ManagerName,
                CreatedDate = portfolio.CreatedDate,
                OwnerName = portfolio.OwnerName,
                AttachedFiles = portfolio.AttachedFiles
            };

            return CreatedAtAction(nameof(GetPortfolio), new { id = portfolio.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePortfolio(int id, [FromBody] UpdatePortfolioDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var portfolio = await _context.Portfolios.FindAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }

            portfolio.Name = dto.Name ?? dto.NameAr ?? portfolio.Name;
            portfolio.Description = dto.Description ?? dto.DescriptionAr ?? portfolio.Description;
            portfolio.Budget = dto.Budget;
            portfolio.Category = dto.Category ?? portfolio.Category;
            portfolio.Status = MapStatusStringToInt(dto.Status);
            portfolio.SponsorName = dto.SponsorName ?? portfolio.SponsorName;
            portfolio.ManagerName = dto.ManagerName ?? portfolio.ManagerName;
            portfolio.OwnerName = !string.IsNullOrEmpty(dto.OwnerName) ? dto.OwnerName : portfolio.OwnerName;
            portfolio.AttachedFiles = dto.AttachedFiles ?? portfolio.AttachedFiles;

            _context.Entry(portfolio).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolio(int id)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (portfolio == null)
                return NotFound();

            if (portfolio.Projects.Any())
            {
                return BadRequest("Cannot delete a portfolio that contains projects.");
            }

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Portfolio deleted successfully." });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetPortfolioStats()
        {
            var portfolios = await _context.Portfolios
                .Include(p => p.Projects)
                .Include(p => p.Programs)
                .ToListAsync();

            var totalBudget = portfolios.Sum(p => p.Budget);
            var totalProjects = portfolios.Sum(p => p.Projects.Count);
            var totalPrograms = portfolios.Sum(p => p.Programs.Count);

            return Ok(new
            {
                TotalPortfolios = portfolios.Count,
                TotalPrograms = totalPrograms,
                TotalProjects = totalProjects,
                TotalBudget = totalBudget
            });
        }

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
