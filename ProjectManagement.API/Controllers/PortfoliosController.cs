using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;


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
    }
}
