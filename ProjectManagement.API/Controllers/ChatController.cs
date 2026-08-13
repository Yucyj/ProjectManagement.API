using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
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

        // GET: api/chat/history/{otherUserId}
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(string otherUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Resolve otherUserId (could be username, email, or database Guid ID)
            var otherUser = await _context.Users.FirstOrDefaultAsync(u => 
                u.Id == otherUserId || 
                u.UserName == otherUserId || 
                u.Email == otherUserId
            );

            if (otherUser == null)
            {
                return NotFound("User not found");
            }

            var resolvedOtherUserId = otherUser.Id;

            // Retrieve conversation between current user and the specified recipient
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == resolvedOtherUserId) ||
                            (m.SenderId == resolvedOtherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender != null ? (m.Sender.NameAr ?? m.Sender.UserName) : "Unknown",
                    m.ReceiverId,
                    ReceiverName = m.Receiver != null ? (m.Receiver.NameAr ?? m.Receiver.UserName) : "Unknown",
                    m.Content,
                    m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }

        // GET: api/chat/conversations
        // Retrieves a list of users the current user has chatted with
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Find unique user IDs that the current user has chatted with
            var sentTo = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId)
                .Select(m => m.ReceiverId)
                .Distinct()
                .ToListAsync();

            var receivedFrom = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var allContactIds = sentTo.Union(receivedFrom).Distinct().ToList();

            var contacts = await _context.Users
                .Where(u => allContactIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    NameAr = u.NameAr ?? u.UserName,
                    NameEn = u.NameEn ?? u.UserName,
                    u.ProfilePhoto
                })
                .ToListAsync();

            return Ok(contacts);
        }
        // GET: api/chat/history
        // Returns all chat messages involving the currently logged-in user
        [HttpGet("history")]
        public async Task<IActionResult> GetAllChatHistory()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    m.SenderId == currentUserId ||
                    m.ReceiverId == currentUserId
                )
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,

                    m.SenderId,
                    SenderName = m.Sender != null
                        ? (m.Sender.NameAr ?? m.Sender.UserName)
                        : "Unknown",

                    m.ReceiverId,
                    ReceiverName = m.Receiver != null
                        ? (m.Receiver.NameAr ?? m.Receiver.UserName)
                        : "Unknown",

                    m.Content,
                    m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
