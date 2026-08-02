using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;

namespace ProjectManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل البرامج مع ميزة الفلترة بالكلمة المفتاحية أو الحالة أو رقم المحفظة (الجدول الرئيسي)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProgramDetailsDto>>> GetPrograms(
            [FromQuery] int? portfolioId,
            [FromQuery] string? keyword,
            [FromQuery] int? status)
        {
            var query = _context.Programs
                .Include(p => p.Portfolio)
                .Include(p => p.Manager)
                .Include(p => p.Projects)
                    .ThenInclude(proj => proj.Tasks)
                .AsQueryable();

            // تطبيق الفلتر الخاص بالمحفظة الحالية إن وُجد
            if (portfolioId.HasValue)
                query = query.Where(p => p.PortfolioId == portfolioId.Value);

            // تطبيق الفلتر بالكلمة المفتاحية (البحث)
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));

            // تطبيق الفلتر بالحالة (Pending, Active, etc.)
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            var programs = await query.Select(p => new ProgramDetailsDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Budget = p.Budget,
                Status = p.Status,
                ProgressPercentage = p.ProgressPercentage,
                SponsorName = p.SponsorName,
                ManagerName = p.Manager != null ? p.Manager.UserName : "Not Assigned",
                PortfolioName = p.Portfolio != null ? p.Portfolio.Name : "N/A",
                PortfolioId = p.PortfolioId,
                CreatedDate = p.CreatedDate,
                AttachedDocumentUrls = !string.IsNullOrEmpty(p.AttachedDocumentUrls)
                    ? p.AttachedDocumentUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                // حماية العدادات من قيم null البرمجية لمنع الاستثناءات
                ProjectsCount = p.Projects != null ? p.Projects.Count : 0,
                TasksCount = p.Projects != null ? p.Projects.Where(proj => proj.Tasks != null).SelectMany(proj => proj.Tasks).Count() : 0
            }).ToListAsync();

            return Ok(programs);
        }

        // 2. جلب تفاصيل برنامج معين لمعاينته (صفحة الـ View والبطاقات ونسبة الـ 70%)
        [HttpGet("{id}")]
        public async Task<ActionResult<ProgramDetailsDto>> GetProgram(int id)
        {
            var program = await _context.Programs
                .Include(p => p.Portfolio)
                .Include(p => p.Manager)
                .Include(p => p.Projects)
                    .ThenInclude(proj => proj.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null)
                return NotFound();

            var result = new ProgramDetailsDto
            {
                Id = program.Id,
                Name = program.Name,
                Description = program.Description,
                Budget = program.Budget,
                Status = program.Status,
                ProgressPercentage = program.ProgressPercentage,
                SponsorName = program.SponsorName,
                ManagerName = program.Manager != null ? program.Manager.UserName : "Not Assigned",
                PortfolioName = program.Portfolio != null ? program.Portfolio.Name : "N/A",
                PortfolioId = program.PortfolioId,
                CreatedDate = program.CreatedDate,
                AttachedDocumentUrls = !string.IsNullOrEmpty(program.AttachedDocumentUrls)
                    ? program.AttachedDocumentUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                // حماية العدادات من قيم null البرمجية لمنع الاستثناءات هنا أيضاً
                ProjectsCount = program.Projects != null ? program.Projects.Count : 0,
                TasksCount = program.Projects != null ? program.Projects.Where(proj => proj.Tasks != null).SelectMany(proj => proj.Tasks).Count() : 0
            };

            return Ok(result);
        }

        // 3. إنشاء برنامج جديد وربطه بالمحفظة الأب
        [HttpPost]
        public async Task<ActionResult<ProjectProgram>> CreateProgram([FromBody] CreateProgramDto dto)
        {
            var program = new ProjectProgram
            {
                Name = dto.Name,
                Description = dto.Description,
                Budget = dto.Budget,
                Status = dto.Status,
                PortfolioId = dto.PortfolioId,
                SponsorName = dto.SponsorName,
                ManagerId = dto.ManagerId,
                CreatedDate = DateTime.UtcNow,
                ProgressPercentage = 0, // يبدأ بـ 0% تلقائياً
                AttachedDocumentUrls = dto.AttachedUrls != null ? string.Join(",", dto.AttachedUrls) : null
            };

            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
        }

        // 4. تعديل بيانات البرنامج (شاشة التعديل التي تملأ البيانات السابقة تلقائياً)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProgram(int id, [FromBody] UpdateProgramDto dto)
        {
            var program = await _context.Programs.FindAsync(id);

            if (program == null)
                return NotFound();

            program.Name = dto.Name;
            program.Description = dto.Description;
            program.Budget = dto.Budget;
            program.Status = dto.Status;
            program.ProgressPercentage = dto.ProgressPercentage;
            program.SponsorName = dto.SponsorName;
            program.ManagerId = dto.ManagerId;
            program.AttachedDocumentUrls = dto.AttachedUrls != null ? string.Join(",", dto.AttachedUrls) : null;

            await _context.SaveChangesAsync();
            return Ok(program);
        }

        // 5. حذف البرنامج مع التحقق الأمني لحماية البيانات التابعة له
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var program = await _context.Programs
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null)
                return NotFound();

            // منع الحذف في حال وجود مشاريع قائمة تحت مظلة هذا البرنامج لحماية سلامة البيانات
            if (program.Projects.Any())
            {
                return BadRequest("Cannot delete a program that currently contains active projects.");
            }

            _context.Programs.Remove(program);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Program deleted successfully." });
        }
    }
}
