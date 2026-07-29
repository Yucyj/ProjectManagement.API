using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using System.Security.Claims;


namespace ProjectManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PortfoliosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfoliosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public async Task<ActionResult<Portfolio>> CreatePortfolio(CreatePortfolioDto dto)
        {
            var portfolio = new Portfolio
            {
                Name = dto.Name,
                Description = dto.Description,
                Budget = dto.Budget,
                Category = dto.Category,
                Status = dto.Status,
                SponsorName = dto.SponsorName,
                ManagerName = dto.ManagerName,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                CreatedDate = DateTime.UtcNow
            };

            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPortfolio), new { id = portfolio.Id }, portfolio);
        }
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePortfolio(int id, UpdatePortfolioDto dto)
        {
            var portfolio = await _context.Portfolios.FindAsync(id);

            if (portfolio == null)
                return NotFound();

            portfolio.Name = dto.Name;
            portfolio.Description = dto.Description;
            portfolio.Budget = dto.Budget;
            portfolio.Category = dto.Category;
            portfolio.Status = dto.Status;
            portfolio.SponsorName = dto.SponsorName;
            portfolio.ManagerName = dto.ManagerName;

            await _context.SaveChangesAsync();

            return Ok(portfolio);
        }
        [HttpDelete("{id}")]
        [Authorize]
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

            return Ok("Portfolio deleted successfully.");
        }
        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetPortfolioStats()
        {
            var portfolios = await _context.Portfolios
                .Include(p => p.Projects)
                .ToListAsync();

            var totalBudget = portfolios.Sum(p => p.Budget);

            var totalProjects = portfolios.Sum(p => p.Projects.Count);

            return Ok(new
            {
                TotalPortfolios = portfolios.Count,
                TotalBudget = totalBudget,
                TotalProjects = totalProjects
            });
        }
    }
}
