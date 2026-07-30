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
                    Status = p.Status,
                    SponsorName = p.SponsorName,
                    ManagerName = p.ManagerName,
                    CreatedDate = p.CreatedDate,
                    OwnerName = p.Owner != null ? p.Owner.UserName : null,
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
                Status = portfolio.Status,
                SponsorName = portfolio.SponsorName,
                ManagerName = portfolio.ManagerName,
                CreatedDate = portfolio.CreatedDate,
                OwnerName = portfolio.Owner?.UserName,
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

            // Resolve current user ID, fallback to first user in Db if null
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                Status = portfolio.Status,
                SponsorName = portfolio.SponsorName,
                ManagerName = portfolio.ManagerName,
                CreatedDate = portfolio.CreatedDate,
                OwnerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Owner"
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

            _context.Entry(portfolio).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolio(int id)
        {
            var portfolio = await _context.Portfolios.FindAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Portfolio deleted successfully" });
        }
    }
}
