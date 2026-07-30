using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ProjectManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var portfolios = await _context.Portfolios.ToListAsync();

            var totalPortfolios = portfolios.Count;
            var activePortfolios = portfolios.Count(p => p.Status == 1); // 1 = Active
            var totalProjects = portfolios.Sum(p => p.Projects != null ? p.Projects.Count : 0);
            
            var completedCount = portfolios.Count(p => p.Status == 2); // 2 = Completed
            var completionRate = totalPortfolios > 0 
                ? (int)Math.Round((double)completedCount / totalPortfolios * 100) 
                : 0;

            var totalBudget = portfolios.Sum(p => p.Budget);

            return Ok(new
            {
                totalPortfolios,
                activePortfolios,
                totalProjects,
                completionRate,
                totalBudget
            });
        }
    }
}
