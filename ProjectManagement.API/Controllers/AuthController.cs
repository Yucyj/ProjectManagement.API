using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.API.Data;
using ProjectManagement.API.DTOs;
using ProjectManagement.API.Models;
using ProjectManagement.API.Services;
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
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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
                PhoneNumber = formattedPhone,
                PhoneNumberConfirmed = true,
                Email = model.Email, 
                NormalizedEmail = model.Email?.ToUpper() ?? string.Empty,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var defaultRole = "Member";
            var roleExists = await _roleManager.RoleExistsAsync(defaultRole);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole(defaultRole));
            }
            await _userManager.AddToRoleAsync(user, defaultRole);

            return Ok(new { message = "تم تسجيل الحساب بنجاح!" });
        }

        // 2. Login: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var formattedPhone = NormalizeAndValidateSaudiPhone(model.PhoneNumber);
            if (formattedPhone == null)
            {
                return BadRequest("رقم الجوال غير صحيح! يجب أن يكون رقم جوال سعودي يبدأ بـ 5 أو 05 أو +966.");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized("رقم الجوال أو الرقم السري غير صحيح!");
            }

            // إذا كان المستخدم مفعلًا للـ 2FA، نقوم بتوليد وإرسال الرمز عبر الإيميل
            if (user.IsTwoFactorEnabled)
            {
                var randomCode = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = randomCode;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5); // صالح لمدة 5 دقائق
                await _userManager.UpdateAsync(user);

                // إرسال الكود الحقيقي عبر الإيميل
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        var subject = "رمز التحقق الثنائي لتسجيل الدخول - ProSync";
                        var body = $@"
                    <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                        <h2 style='color: #007bff;'>مرحباً {user.UserName}،</h2>
                        <p style='font-size: 1.1rem; color: #4a5568;'>تلقينا محاولة تسجيل دخول إلى حسابك على منصة ProSync.</p>
                        <p style='font-size: 1.1rem; color: #4a5568;'>رمز التحقق (OTP) الخاص بك هو:</p>
                        <div style='background: #f7fafc; padding: 15px; border-radius: 8px; text-align: center; font-size: 1.8rem; font-weight: bold; letter-spacing: 4px; color: #1a2b4c; margin: 20px 0;'>
                            {randomCode}
                        </div>
                        <p style='font-size: 0.95rem; color: #718096;'>هذا الرمز صالح لمدة 5 دقائق فقط. إذا لم تقم بهذا الطلب، يرجى تجاهل هذه الرسالة.</p>
                    </div>";

                        await _emailService.SendEmailAsync(user.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[2FA EMAIL ERROR]: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    requiresTwoFactor = true,
                    userId = user.Id,
                    message = "تم إرسال رمز التحقق إلى بريدك الإلكتروني بنجاح!"
                });
            }

            // إذا لم يكن مفعلًا للـ 2FA، يتم إصدار الـ JWT Token النهائي مباشرة
            var roles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var expiration = DateTime.UtcNow.AddHours(_configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 600);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: authClaims,
                expires: expiration,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                id = user.Id,
                userId = user.Id,
                username = user.UserName,
                phoneNumber = user.PhoneNumber,
                email = user.Email,
                token = tokenValue,
                expiration,
                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    phoneNumber = user.PhoneNumber,
                    email = user.Email,
                    roles
                }
            });
        }
        // 2.5. Verify Login 2FA: api/Auth/verify-login-2fa
        [HttpPost("verify-login-2fa")]
        public async Task<IActionResult> VerifyLogin2Fa([FromBody] VerifyLoginTwoFactorDto model)
        {
            // ملاحظة: تأكدي أن الـ DTO هنا يستقبل (UserId و Code) أو (Email و Code) بناءً على ما ترسلينه من الفرونتاند.
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return Unauthorized("بيانات غير صالحة!");
            }

            if (string.IsNullOrEmpty(user.TwoFactorCode) ||
                user.TwoFactorCode != model.Code ||
                user.TwoFactorCodeExpiry == null ||
                user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                return BadRequest("رمز التحقق غير صحيح أو انتهت صلاحيته!");
            }

            // مسح الرمز بعد استخدامه بنجاح لضمان الأمان
            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            await _userManager.UpdateAsync(user);

            // إصدار الـ JWT Token النهائي للمستخدم
            var roles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var expiration = DateTime.UtcNow.AddHours(_configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 600);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: authClaims,
                expires: expiration,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                id = user.Id,
                userId = user.Id,
                username = user.UserName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                token = tokenValue,
                expiration,
                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    roles
                }
            });
        }
        // 14. Toggle 2FA status: api/Auth/toggle-2fa
        [HttpPost("toggle-2fa")]
        public async Task<IActionResult> ToggleTwoFactor([FromBody] ToggleTwoFactorDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            user.IsTwoFactorEnabled = model.Enable;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = model.Enable ? "تم تفعيل التحقق بخطوتين بنجاح!" : "تم تعطيل التحقق بخطوتين بنجاح!" });
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
        // 4. Change Password: api/Auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest("البريد الإلكتروني مطلوب!");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            IdentityResult result;
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            }
            else
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            }

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "تم تغيير الرقم السري بنجاح!" });
        }

        // 5. Forgot Password: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest("البريد الإلكتروني مطلوب!");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            // Generate an OTP token using the Email token provider
            var token = await _userManager.GenerateUserTokenAsync(user, "Email", "ResetPassword");

            // Try sending the OTP email
            try
            {
                var subject = "رمز التحقق لإعادة تعيين كلمة المرور - ProSync";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                        <h2 style='color: #007bff;'>مرحباً {user.UserName}،</h2>
                        <p style='font-size: 1.1rem; color: #4a5568;'>لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بك على منصة ProSync.</p>
                        <p style='font-size: 1.1rem; color: #4a5568;'>رمز التحقق (OTP) الخاص بك هو:</p>
                        <div style='background: #f7fafc; padding: 15px; border-radius: 8px; text-align: center; font-size: 1.8rem; font-weight: bold; letter-spacing: 4px; color: #1a2b4c; margin: 20px 0;'>
                            {token}
                        </div>
                        <p style='font-size: 0.95rem; color: #718096;'>هذا الرمز صالحة لمدة محدودة. إذا لم تقم بطلب إعادة التعيين بنفسك، يرجى تجاهل هذا البريد الإلكتروني.</p>
                        <hr style='border: none; border-top: 1px solid #edf2f7; margin: 20px 0;' />
                        <p style='font-size: 0.85rem; color: #a0aec0; text-align: center;'>فريق حماية ProSync Security</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email!, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL SEND FAILURE]: {ex.Message}");
            }

            return Ok(new
            {
                Message = "تم توليد رمز إعادة تعيين كلمة المرور بنجاح وإرساله للبريد الإلكتروني!"
            });
        }

        // 5.5. Verify OTP: api/Auth/verify-otp
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            var isValid = await _userManager.VerifyUserTokenAsync(user, "Email", "ResetPassword", model.Token);
            if (!isValid)
            {
                return BadRequest("رمز التحقق غير صحيح!");
            }

            return Ok(new { success = true, message = "رمز التحقق صحيح!" });
        }

        // 6. Reset Password: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest("البريد الإلكتروني مطلوب!");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("المستخدم غير موجود!");
            }

            // Verify the token using the Email token provider
            var isValid = await _userManager.VerifyUserTokenAsync(user, "Email", "ResetPassword", model.Token);
            if (!isValid)
            {
                return BadRequest(new[] { new { code = "InvalidToken", description = "رمز إعادة التعيين غير صحيح أو انتهت صلاحيته!" } });
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors);
                }
            }

            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "تم إعادة تعيين كلمة المرور الجديدة بنجاح!" });
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
                PhoneNumberConfirmed = true,
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
            user.PhoneNumberConfirmed = true;
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
