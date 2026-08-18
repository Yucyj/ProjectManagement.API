using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ChatController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: api/chat/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadChatFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // 10 MB Limit
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest("File size exceeds the 10MB limit.");
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "chat");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/chat/{uniqueFileName}";
            var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                fileUrl = fileUrl,
                fileName = file.FileName,
                fileType = isImage ? "image" : "file",
                fileSize = $"{Math.Round(file.Length / 1024.0, 1)} KB"
            });
        }

        // GET: api/chat/history
        // Returns the company-wide chat history
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(m => m.Sender)
                .Where(m => m.ReceiverId == null)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender != null
                        ? (m.Sender.NameAr ?? m.Sender.UserName)
                        : "Unknown",
                    m.Content,
                    m.FileUrl,
                    m.FileName,
                    m.FileType,
                    m.Timestamp,

                    ReplyToMessage = m.ReplyToMessage == null
                        ? null
                        : new
                        {
                            Id = m.ReplyToMessage.Id,
                            SenderName = m.ReplyToMessage.Sender != null
                                ? (m.ReplyToMessage.Sender.NameAr ?? m.ReplyToMessage.Sender.UserName)
                                : "Unknown",
                            Content = m.ReplyToMessage.Content
                        },

                    Reactions = m.Reactions.Select(r => new
                    {
                        r.UserId,
                        UserName = r.User != null
                            ? (r.User.NameAr ?? r.User.UserName)
                            : "Unknown",
                        r.Emoji
                    }).ToList(),

                    ReadStates = _context.MessageReadStates
                        .Where(r => r.MessageId == m.Id)
                        .Select(r => new
                        {
                            r.UserId,
                            r.ReadAt
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(messages);
        }

        // GET: api/chat/online-users
        // Returns the list of currently online user IDs with presence metadata
        [HttpGet("online-users")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var users = await _context.Users.ToListAsync();

            var result = users.Select(u => {
                var isOnline = ProjectManagement.API.Hubs.ChatConnectionManager.OnlineUsers.ContainsKey(u.Id);

                DateTime? lastSeen = null;
                if (!isOnline && ProjectManagement.API.Hubs.ChatConnectionManager.LastSeenTimes.TryGetValue(u.Id, out var seenTime))
                {
                    lastSeen = seenTime;
                }

                DateTime? lastActivity = null;
                if (ProjectManagement.API.Hubs.ChatConnectionManager.LastActivityTimes.TryGetValue(u.Id, out var activityTime))
                {
                    lastActivity = activityTime;
                }

                return new
                {
                    userId = u.Id,
                    userName = u.NameAr ?? u.UserName ?? "Unknown",
                    profilePhoto = string.IsNullOrEmpty(u.ProfilePhoto)
                        ? "/images/default-profile.png"
                        : u.ProfilePhoto,
                    isOnline = isOnline,
                    lastSeenUtc = lastSeen,
                    lastActivityUtc = lastActivity
                };
            }).ToList();

            return Ok(result);
        }
    }
}