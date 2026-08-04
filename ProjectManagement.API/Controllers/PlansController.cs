using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Replace with your actual DbContext name

        public PlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Plan>>> GetAllPlans()
        {
            return await _context.Plans
                .Include(p => p.Milestones)
                .Include(p => p.Deliverables)
                .ToListAsync();
        }

        [HttpGet("details/{id}")]
        public async Task<ActionResult<Plan>> GetPlanDetails(int id)
        {
            var plan = await _context.Plans
                .Include(p => p.Milestones)
                .Include(p => p.Deliverables)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();
            return plan;
        }

        [HttpPost("create")]
        public async Task<ActionResult<Plan>> CreatePlan([FromBody] Plan plan)
        {
            plan.LastUpdated = DateTime.UtcNow;
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPlanDetails), new { id = plan.Id }, plan);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] Plan plan)
        {
            if (id != plan.Id) return BadRequest();

            plan.LastUpdated = DateTime.UtcNow;
            _context.Entry(plan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Plans.AnyAsync(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null) return NotFound();

            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}