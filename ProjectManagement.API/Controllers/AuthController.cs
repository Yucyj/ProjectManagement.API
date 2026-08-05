using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }
        // Helper: Validate and normalize Saudi mobile numbers to +9665xxxxxxxx
        private string? NormalizeAndValidateSaudiPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Remove any spaces, dashes, or parentheses
            var cleaned = phoneNumber.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // If it starts with +966
            if (cleaned.StartsWith("+966"))
            {
                var rest = cleaned.Substring(4);
                if (rest.Length == 9 && rest.All(char.IsDigit) && rest.StartsWith("5"))
                {
                    return cleaned;
                }
            }
            // If it starts with 966
            else if (cleaned.StartsWith("966"))
            {
                var rest = cleaned.Substring(3);
                if (rest.Length == 9 && rest.All(char.IsDigit) && rest.StartsWith("5"))
                {
                    return "+" + cleaned;
                }
            }
            // If it starts with 05
            else if (cleaned.StartsWith("05"))
            {
                var rest = cleaned.Substring(1); // remove leading 0, keeping 5xxxxxxxxx
                if (rest.Length == 9 && rest.All(char.IsDigit))
                {
                    return "+966" + rest;
                }
            }
            // If it starts with 5 (length 9)
            else if (cleaned.StartsWith("5") && cleaned.Length == 9 && cleaned.All(char.IsDigit))
            {
                return "+966" + cleaned;
            }

            return null; // Invalid Saudi mobile format
        }

        // 1. Register: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح! يجب أن يكون رقم جوال سعودي يبدأ بـ 5 أو 05 أو +966 ويحتوي على أرقام فقط.");
            }

            // Check if phone number already exists
            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == formattedPhone);
            if (phoneExists)
                return BadRequest("رقم الجوال هذا مسجل مسبقاً!");

            // Check if username already exists
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return BadRequest("اسم المستخدم هذا مسجل مسبقاً!");

            var user = new ApplicationUser
            {
                UserName = model.Username,
                PhoneNumber = formattedPhone
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("تم تسجيل الحساب بنجاح!");
        }

        // 2. Login: api/Auth/login
    
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var formattedPhone =
                NormalizeAndValidateSaudiPhone(model.PhoneNumber);

            if (formattedPhone == null)
            {
                return BadRequest(
                    "رقم الجوال غير صحيح! يجب أن يكون رقم جوال سعودي يبدأ بـ 5 أو 05 أو +966 ويحتوي على أرقام فقط.");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);

            if (user == null ||
                !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(
                    "رقم الجوال أو الرقم السري غير صحيح!");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            user.Id),

        new Claim(
            ClaimTypes.Name,
            user.UserName ?? string.Empty),

        new Claim(
            ClaimTypes.MobilePhone,
            user.PhoneNumber ?? string.Empty),

        new Claim(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString())
    };

            foreach (var role in roles)
            {
                authClaims.Add(
                    new Claim(ClaimTypes.Role, role));
            }

            // قراءة إعدادات JWT من appsettings.json
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var expiryHours =
                _configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 3;

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "JWT settings are missing in appsettings.json."
                    });
            }

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var expiration =
                DateTime.UtcNow.AddHours(expiryHours);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: authClaims,
                notBefore: DateTime.UtcNow,
                expires: expiration,
                signingCredentials: new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256)
            );

            var tokenValue =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                id = user.Id,
                userId = user.Id,
                username = user.UserName,
                phoneNumber = user.PhoneNumber,
                token = tokenValue,
                expiration,

                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    phoneNumber = user.PhoneNumber,
                    nameEn = user.NameEn,
                    nameAr = user.NameAr,
                    titleEn = user.TitleEn,
                    titleAr = user.TitleAr,
                    companyEn = user.CompanyEn,
                    companyAr = user.CompanyAr,
                    profilePhoto = user.ProfilePhoto,
                    backgroundPhoto = user.BackgroundPhoto,
                    aboutAr = user.AboutAr,
                    aboutEn = user.AboutEn,
                    roles
                }
            });
        }

        // 3. Get All Users: api/Auth/all-users
        [HttpGet("all-users")]
        public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetAllUsers()
        {
            var usersList = await _userManager.Users.ToListAsync();
            var result = new List<UserListItemDto>();

            foreach (var u in usersList)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var roleName = roles.FirstOrDefault() ?? "Member";

                result.Add(new UserListItemDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    Role = roleName,
                    NameAr = u.NameAr,
                    NameEn = u.NameEn,
                    TitleAr = u.TitleAr,
                    TitleEn = u.TitleEn,
                    CreatedDate = u.CreatedDate,
                    IsActive = u.IsActive
                });
            }

            return Ok(result);
        }

        // 4. Change Password: api/Auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح!");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("تم تغيير الرقم السري بنجاح!");
        }

        // 5. Forgot Password: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح!");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            // Generate an OTP token using the Phone token provider
            var token = await _userManager.GenerateUserTokenAsync(user, "Phone", "ResetPassword");

            return Ok(new
            {
                Message = "تم توليد رمز إعادة تعيين كلمة المرور بنجاح وارتباطه برقم الجوال!",
                Token = token
            });
        }

        // 6. Reset Password: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح!");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            // Verify the token using the Phone token provider
            var isValid = await _userManager.VerifyUserTokenAsync(user, "Phone", "ResetPassword", model.Token);
            if (!isValid)
            {
                return BadRequest(new[] { new { code = "InvalidToken", description = "رمز إعادة التعيين غير صحيح أو انتهت صلاحيته!" } });
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("تم إعادة تعيين كلمة المرور الجديدة بنجاح!");
        }

        // 7. Create Role: api/Auth/create-role
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto model)
        {
            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);

            if (roleExists)
            {
                return BadRequest("Role already exists");
            }

            var role = new IdentityRole(model.RoleName);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("Role created successfully");
        }

        // 8. Get All Roles: api/Auth/roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name
                })
                .ToListAsync();

            return Ok(roles);
        }

        // 9. Create Super Admin: api/Auth/create-superadmin
        [HttpPost("create-superadmin")]
        public async Task<IActionResult> CreateSuperAdmin([FromBody] CreateSuperAdminDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح!");
            }

            var roleExists = await _roleManager.RoleExistsAsync("SuperAdmin");

            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            }

            var userExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == formattedPhone);

            if (userExists)
            {
                return BadRequest("User with this phone number already exists");
            }

            var user = new ApplicationUser
            {
                UserName = model.Username,
                PhoneNumber = formattedPhone,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(user, "SuperAdmin");

            return Ok("SuperAdmin created successfully");
        }

        // 10. Delete User: api/Auth/delete-user/{userId}
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok(new { Message = "تم حذف المستخدم بنجاح!" });
        }

        // 11. Create User: api/Auth/create-user
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var formattedPhone = NormalizeAndValidateSaudiPhone(dto.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح! يجب أن يكون رقم جوال سعودي يبدأ بـ 5 أو 05 أو +966.");
            }

            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == formattedPhone);
            if (phoneExists)
                return BadRequest("رقم الجوال هذا مسجل مسبقاً!");

            var userExists = await _userManager.FindByNameAsync(dto.Username);
            if (userExists != null)
                return BadRequest("اسم المستخدم هذا مسجل مسبقاً!");

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                PhoneNumber = formattedPhone,
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                TitleAr = dto.TitleAr,
                TitleEn = dto.TitleEn,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Assign role
            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole(dto.Role));
            }
            await _userManager.AddToRoleAsync(user, dto.Role);

            // Handle association if provided (OwnerId for Portfolio, ManagerId for Program/Project)
            if (dto.PortfolioId.HasValue)
            {
                var portfolio = await _context.Portfolios.FindAsync(dto.PortfolioId.Value);
                if (portfolio != null)
                {
                    portfolio.OwnerId = user.Id;
                    _context.Entry(portfolio).State = EntityState.Modified;
                }
            }

            if (dto.ProgramId.HasValue)
            {
                var program = await _context.Programs.FindAsync(dto.ProgramId.Value);
                if (program != null)
                {
                    program.ManagerId = user.Id;
                    _context.Entry(program).State = EntityState.Modified;
                }
            }

            if (dto.ProjectId.HasValue)
            {
                var project = await _context.Projects.FindAsync(dto.ProjectId.Value);
                if (project != null)
                {
                    project.ManagerId = user.Id;
                    _context.Entry(project).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم إنشاء المستخدم بنجاح!", UserId = user.Id });
        }

        // 12. Update User: api/Auth/update-user/{userId}
        [HttpPut("update-user/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            var formattedPhone = NormalizeAndValidateSaudiPhone(dto.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح!");
            }

            // Verify phone unique
            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == formattedPhone && u.Id != userId);
            if (phoneExists)
                return BadRequest("رقم الجوال هذا مسجل مسبقاً لمستخدم آخر!");

            if (!string.IsNullOrWhiteSpace(dto.Username) && user.UserName != dto.Username)
            {
                var userExists = await _userManager.FindByNameAsync(dto.Username);
                if (userExists != null)
                {
                    return BadRequest("اسم المستخدم هذا مسجل مسبقاً لمستخدم آخر!");
                }
                user.UserName = dto.Username;
                user.NormalizedUserName = dto.Username.ToUpper();
            }

            user.Email = dto.Email;
            user.NormalizedEmail = dto.Email.ToUpper();
            user.PhoneNumber = formattedPhone;
            user.NameAr = dto.NameAr;
            user.NameEn = dto.NameEn;
            user.TitleAr = dto.TitleAr;
            user.TitleEn = dto.TitleEn;
            user.IsActive = dto.IsActive;

            // Optional password update
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
                if (!passResult.Succeeded)
                {
                    return BadRequest(passResult.Errors);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Role updates
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole(dto.Role));
            }
            await _userManager.AddToRoleAsync(user, dto.Role);

            // Handle association updates if provided
            if (dto.PortfolioId.HasValue)
            {
                var portfolio = await _context.Portfolios.FindAsync(dto.PortfolioId.Value);
                if (portfolio != null)
                {
                    portfolio.OwnerId = user.Id;
                    _context.Entry(portfolio).State = EntityState.Modified;
                }
            }

            if (dto.ProgramId.HasValue)
            {
                var program = await _context.Programs.FindAsync(dto.ProgramId.Value);
                if (program != null)
                {
                    program.ManagerId = user.Id;
                    _context.Entry(program).State = EntityState.Modified;
                }
            }

            if (dto.ProjectId.HasValue)
            {
                var project = await _context.Projects.FindAsync(dto.ProjectId.Value);
                if (project != null)
                {
                    project.ManagerId = user.Id;
                    _context.Entry(project).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تحديث بيانات المستخدم بنجاح!" });
        }

        // 13. User Profile Details: api/Auth/user-profile/{id}
        [HttpGet("user-profile/{id}")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            var roles = await _userManager.GetRolesAsync(u);
            var roleName = roles.FirstOrDefault() ?? "Member";

            var dto = new UserProfileDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = roleName,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                TitleAr = u.TitleAr,
                TitleEn = u.TitleEn,
                CreatedDate = u.CreatedDate,
                IsActive = u.IsActive
            };

            // 1. Portfolios owned by user
            var portfolios = await _context.Portfolios
                .Where(p => p.OwnerId == id)
                .Select(p => new UserPortfolioDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category ?? "Execution",
                    ProgramsCount = _context.Programs.Count(pr => pr.PortfolioId == p.Id),
                    ProjectsCount = _context.Projects.Count(proj => proj.PortfolioId == p.Id),
                    Progress = 0,
                    Status = p.Status == 1 ? "Active" : p.Status == 2 ? "Completed" : p.Status == 3 ? "Pending" : "Rejected"
                })
                .ToListAsync();

            dto.Portfolios = portfolios;

            // 2. Programs managed by user
            var programs = await _context.Programs
                .Where(pr => pr.ManagerId == id)
                .Select(pr => new UserProgramDto
                {
                    Id = pr.Id,
                    Name = pr.Name,
                    Category = "Execution",
                    ProjectsCount = _context.Projects.Count(proj => proj.ProgramId == pr.Id),
                    Progress = pr.ProgressPercentage,
                    Status = pr.Status == 1 ? "Active" : pr.Status == 2 ? "Completed" : pr.Status == 3 ? "Pending" : "Rejected"
                })
                .ToListAsync();

            dto.Programs = programs;

            // 3. Projects managed by user
            var projects = await _context.Projects
                .Where(proj => proj.ManagerId == id)
                .Select(proj => new UserProjectDto
                {
                    Id = proj.Id,
                    Name = proj.Name,
                    Category = "Execution",
                    TasksCount = _context.Tasks.Count(t => t.ProjectId == proj.Id),
                    Progress = 0, // mock project progress
                    Status = proj.Status == 1 ? "Active" : proj.Status == 2 ? "Completed" : proj.Status == 3 ? "Pending" : "Rejected"
                })
                .ToListAsync();

            dto.Projects = projects;

            return Ok(dto);
        }
    }
}
