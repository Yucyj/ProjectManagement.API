using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
using System.Threading.Tasks;

namespace ProjectManagement.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string content)
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId) ||
                string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            // جلب بيانات المرسل
            var sender = await _context.Users.FindAsync(senderId);

            if (sender == null)
            {
                return;
            }

            var senderName = sender.NameAr ?? sender.UserName ?? "Unknown";
            var senderPhoto = sender.ProfilePhoto ?? "";

            // حفظ الرسالة
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = null,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // إرسال الرسالة للجميع
            await Clients.All.SendAsync(
                "ReceiveMessage",
                senderId,
                senderName,
                senderPhoto,
                content.Trim(),
                message.Timestamp
            );
        }
    }
}