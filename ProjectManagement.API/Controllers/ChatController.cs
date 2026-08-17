using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using System.Security.Claims;

namespace ProjectManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
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

                // جلب الرسالة التي تم الرد عليها + صاحبها
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(m => m.Sender)

                .Where(m => m.ReceiverId == null)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    // المعلومات الموجودة سابقًا
                    m.Id,
                    m.SenderId,

                    SenderName = m.Sender != null
                        ? (m.Sender.NameAr ?? m.Sender.UserName)
                        : "Unknown",

                    m.Content,
                    m.Timestamp,

                    // الرسالة التي يرد عليها المستخدم
                    ReplyToMessage = m.ReplyToMessage == null
                        ? null
                        : new
                        {
                            Id = m.ReplyToMessage.Id,

                            SenderName = m.ReplyToMessage.Sender != null
                                ? (m.ReplyToMessage.Sender.NameAr ??
                                   m.ReplyToMessage.Sender.UserName)
                                : "Unknown",

                            Content = m.ReplyToMessage.Content
                        },

                    // الـ reactions
                    Reactions = m.Reactions.Select(r => new
                    {
                        r.UserId,

                        UserName = r.User != null
                            ? (r.User.NameAr ?? r.User.UserName)
                            : "Unknown",

                        r.Emoji
                    }).ToList(),

                    // الجديد فقط
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