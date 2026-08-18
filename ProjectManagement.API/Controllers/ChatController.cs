using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Hubs;
using ProjectManagement.API.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectManagement.API.Controllers
{
    public class EditMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, IWebHostEnvironment env, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _env = env;
            _hubContext = hubContext;
        }

        // POST: api/chat/upload
        // Saves file, creates database record, and broadcasts live via SignalR
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadChatFile(
            IFormFile file,
            [FromForm] string? content = null,
            [FromForm] int? replyToMessageId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("File size exceeds 10MB limit.");

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
            var fileType = isImage ? "image" : "file";

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            var senderName = sender != null ? (sender.NameAr ?? sender.UserName ?? "Unknown") : "Unknown";
            var senderPhoto = string.IsNullOrEmpty(sender?.ProfilePhoto) ? "/images/default-profile.png" : sender.ProfilePhoto;

            // 1. Create and save message entity in Database
            var message = new ChatMessage
            {
                SenderId = currentUserId,
                ReceiverId = null,
                Content = (content ?? string.Empty).Trim(),
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileType = fileType,
                ReplyToMessageId = replyToMessageId,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // 2. Broadcast immediately to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = message.SenderId,
                senderName = senderName,
                senderPhoto = senderPhoto,
                receiverId = (string?)null,
                receiverName = (string?)null,
                content = message.Content,
                fileUrl = message.FileUrl,
                fileName = message.FileName,
                fileType = message.FileType,
                replyToMessageId = message.ReplyToMessageId,
                timestamp = message.Timestamp,
                isEdited = false,
                isDeleted = false,
                reactions = Array.Empty<object>()
            });

            return Ok(new
            {
                id = message.Id,
                fileUrl = fileUrl,
                fileName = file.FileName,
                fileType = fileType,
                fileSize = $"{Math.Round(file.Length / 1024.0, 1)} KB",
                timestamp = message.Timestamp
            });
        }

        // GET: api/chat/history
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

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
                    SenderPhoto = string.IsNullOrEmpty(m.Sender.ProfilePhoto)
                        ? "/images/default-profile.png"
                        : m.Sender.ProfilePhoto,
                    m.Content,
                    m.FileUrl,
                    m.FileName,
                    m.FileType,
                    m.Timestamp,
                    m.IsEdited,
                    m.EditedAt,
                    m.IsDeleted,

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

        // PUT: api/chat/messages/{id}
        [HttpPut("messages/{id}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] EditMessageDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Message content cannot be empty.");

            var message = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
                return NotFound("Message not found.");

            if (message.SenderId != currentUserId)
                return Forbid();

            if (message.IsDeleted)
                return BadRequest("Cannot edit a deleted message.");

            message.Content = dto.Content.Trim();
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("MessageEdited", new
            {
                messageId = message.Id,
                newContent = message.Content,
                isEdited = message.IsEdited,
                editedAt = message.EditedAt
            });

            return Ok(new { message = "Message updated successfully." });
        }

        // DELETE: api/chat/messages/{id}
        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var message = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
                return NotFound("Message not found.");

            if (message.SenderId != currentUserId)
                return Forbid();

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.Content = " „ Õ–› Â–Â «·—”«·…";
            message.FileUrl = null;
            message.FileName = null;
            message.FileType = null;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("MessageDeleted", new
            {
                messageId = message.Id
            });

            return Ok(new { message = "Message deleted successfully." });
        }

        // GET: api/chat/online-users
        [HttpGet("online-users")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var users = await _context.Users.ToListAsync();

            var result = users.Select(u => {
                var isOnline = ChatConnectionManager.OnlineUsers.ContainsKey(u.Id);

                DateTime? lastSeen = null;
                if (!isOnline && ChatConnectionManager.LastSeenTimes.TryGetValue(u.Id, out var seenTime))
                {
                    lastSeen = seenTime;
                }

                DateTime? lastActivity = null;
                if (ChatConnectionManager.LastActivityTimes.TryGetValue(u.Id, out var activityTime))
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