using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using ProjectManagement.API.Services;
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
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PortfoliosController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
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
                    OwnerId = p.OwnerId,
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
                OwnerId = portfolio.OwnerId,
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

            // إرسال تنبيهات البريد الإلكتروني للملاك والمدراء المعينين
            try
            {
                var users = await _context.Users.ToListAsync();
                Func<string, ApplicationUser?> findUser = (name) =>
                {
                    if (string.IsNullOrEmpty(name)) return null;
                    var normalized = name.Trim().ToLower();
                    Console.WriteLine($"[DIAGNOSTIC]: Searching for user match with input: '{name}' (normalized: '{normalized}')");
                    var matched = users.FirstOrDefault(u => 
                        (u.NameAr != null && u.NameAr.Trim().ToLower() == normalized) ||
                        (u.NameEn != null && u.NameEn.Trim().ToLower() == normalized) ||
                        (u.UserName != null && u.UserName.Trim().ToLower() == normalized) ||
                        (u.Email != null && u.Email.Trim().ToLower() == normalized) ||
                        (u.Id != null && u.Id.Trim().ToLower() == normalized)
                    );
                    if (matched != null)
                    {
                        Console.WriteLine($"[DIAGNOSTIC]: Found match! User ID: '{matched.Id}', UserName: '{matched.UserName}', Email: '{matched.Email}'");
                    }
                    else
                    {
                        Console.WriteLine($"[DIAGNOSTIC]: No match found for input '{name}' in database users list.");
                    }
                    return matched;
                };

                var owner = findUser(portfolio.OwnerName);
                var manager = findUser(portfolio.ManagerName);
                var sponsor = findUser(portfolio.SponsorName);

                Console.WriteLine($"[DIAGNOSTIC]: Owner search result is null: {owner == null}");
                Console.WriteLine($"[DIAGNOSTIC]: Manager search result is null: {manager == null}");
                Console.WriteLine($"[DIAGNOSTIC]: Sponsor search result is null: {sponsor == null}");

                var frontendBase = _configuration["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
                var portfolioDetailsUrl = $"{frontendBase}/portfolios/details/{portfolio.Id}";

                // 1. إرسال للمالك
                if (owner != null && !string.IsNullOrEmpty(owner.Email))
                {
                    var subject = $"تم تعيينك كمالك لمحفظة جديدة: {portfolio.Name}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                            <h2 style='color: #112b50;'>مرحباً {owner.UserName}،</h2>
                            <p style='font-size: 1.1rem; color: #334155;'>لقد تم إنشاء محفظة جديدة وتعيينك كمالك لها.</p>
                            <div style='background: #f8fafc; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #e4e7eb;'>
                                <strong>اسم المحفظة:</strong> {portfolio.Name}<br>
                                <strong>ميزانية المحفظة:</strong> {portfolio.Budget:N2} ريال سعودي<br>
                                <strong>التصنيف:</strong> {portfolio.Category}
                            </div>
                            <p style='font-size: 1rem; color: #475569;'>يرجى تسجيل الدخول إلى المنصة لمتابعة حالة المحفظة والبدء في إنشاء البرامج والمشاريع التابعة لها.</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{portfolioDetailsUrl}' style='display: inline-block; background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 1rem;'>عرض تفاصيل المحفظة</a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #edf2f7; margin: 20px 0;' />
                            <p style='font-size: 0.85rem; color: #94a3b8; text-align: center;'>هذا البريد تم إرساله تلقائياً من نظام إدارة المشاريع ProSync</p>
                        </div>";
                    await _emailService.SendEmailAsync(owner.Email, subject, body);
                }

                // 2. إرسال للمدير
                if (manager != null && !string.IsNullOrEmpty(manager.Email) && manager.Id != owner?.Id)
                {
                    var subject = $"تم تعيينك كمدير لمحفظة جديدة: {portfolio.Name}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                            <h2 style='color: #112b50;'>مرحباً {manager.UserName}،</h2>
                            <p style='font-size: 1.1rem; color: #334155;'>لقد تم إنشاء محفظة جديدة وتعيينك كمدير للمحفظة.</p>
                            <div style='background: #f8fafc; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #e4e7eb;'>
                                <strong>اسم المحفظة:</strong> {portfolio.Name}<br>
                                <strong>ميزانية المحفظة:</strong> {portfolio.Budget:N2} ريال سعودي<br>
                                <strong>التصنيف:</strong> {portfolio.Category}
                            </div>
                            <p style='font-size: 1rem; color: #475569;'>يرجى تسجيل الدخول لمتابعة الخطة التشغيلية للمحفظة.</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{portfolioDetailsUrl}' style='display: inline-block; background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 1rem;'>عرض تفاصيل المحفظة</a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #edf2f7; margin: 20px 0;' />
                            <p style='font-size: 0.85rem; color: #94a3b8; text-align: center;'>هذا البريد تم إرساله تلقائياً من نظام إدارة المشاريع ProSync</p>
                        </div>";
                    await _emailService.SendEmailAsync(manager.Email, subject, body);
                }

                // 3. إرسال للراعي
                if (sponsor != null && !string.IsNullOrEmpty(sponsor.Email) && sponsor.Id != owner?.Id && sponsor.Id != manager?.Id)
                {
                    var subject = $"تم تعيينك كراعي لمحفظة جديدة: {portfolio.Name}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                            <h2 style='color: #112b50;'>مرحباً {sponsor.UserName}،</h2>
                            <p style='font-size: 1.1rem; color: #334155;'>لقد تم إنشاء محفظة جديدة وتعيينك كراعي رسمي لها.</p>
                            <div style='background: #f8fafc; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #e4e7eb;'>
                                <strong>اسم المحفظة:</strong> {portfolio.Name}<br>
                                <strong>ميزانية المحفظة:</strong> {portfolio.Budget:N2} ريال سعودي<br>
                                <strong>التصنيف:</strong> {portfolio.Category}
                            </div>
                            <p style='font-size: 1rem; color: #475569;'>يرجى تسجيل الدخول لمتابعة التقارير التشغيلية للمحفظة.</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{portfolioDetailsUrl}' style='display: inline-block; background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 1rem;'>عرض تفاصيل المحفظة</a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #edf2f7; margin: 20px 0;' />
                            <p style='font-size: 0.85rem; color: #94a3b8; text-align: center;'>هذا البريد تم إرساله تلقائياً من نظام إدارة المشاريع ProSync</p>
                        </div>";
                    await _emailService.SendEmailAsync(sponsor.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PORTFOLIO CREATION EMAIL ERROR]: Failed to dispatch emails. Error: {ex.Message}");
            }

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
            portfolio.AttachedFiles = dto.AttachedFiles;

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
