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
                    m.Timestamp,
                    Reactions = m.Reactions.Select(r => new
                    {
                        r.UserId,
                        UserName = r.User != null ? (r.User.NameAr ?? r.User.UserName) : "Unknown",
                        r.Emoji
                    }).ToList()
                })
                .ToListAsync();

            return Ok(messages);
        }

        // GET: api/chat/online-users
        // Returns the list of currently online user IDs
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            var onlineUserIds = ProjectManagement.API.Hubs.ChatConnectionManager.OnlineUsers.Keys.ToList();
            return Ok(onlineUserIds);
        }
    }
}