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
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    UserName = u.UserName,
                    PhoneNumber = u.PhoneNumber,
                    NameAr = u.NameAr,
                    NameEn = u.NameEn,
                    ProfilePhoto = u.ProfilePhoto,
                    TitleAr = u.TitleAr,
                    TitleEn = u.TitleEn,
                    Role = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .FirstOrDefault() ?? "Member"
                })
                .ToListAsync();

            return Ok(users);
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
    }
}
