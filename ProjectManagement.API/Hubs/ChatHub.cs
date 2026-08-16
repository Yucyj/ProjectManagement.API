using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;

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

            var sender = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == senderId);

            if (sender == null)
            {
                return;
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = null,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var response = new
            {
                message.Id,
                message.SenderId,
                SenderName = sender.NameAr ?? sender.UserName ?? "Unknown",

                SenderPhoto = string.IsNullOrEmpty(sender.ProfilePhoto)
                    ? "/images/default-profile.png"
                    : sender.ProfilePhoto,

                message.ReceiverId,
                ReceiverName = (string?)null,
                message.Content,
                message.Timestamp
            };

            await Clients.All.SendAsync(
                "ReceiveMessage",
                response
            );
        }


        public async Task StartTyping()
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId))
                return;

            var sender = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == senderId);

            if (sender == null)
                return;

            var senderName = sender.NameAr ?? sender.UserName ?? "Unknown";

            await Clients.Others.SendAsync(
                "UserTyping",
                senderName,
                true
            );
        }

        public async Task StopTyping()
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId))
                return;

            var sender = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == senderId);

            if (sender == null)
                return;

            var senderName = sender.NameAr ?? sender.UserName ?? "Unknown";

            await Clients.Others.SendAsync(
                "UserTyping",
                senderName,
                false
            );
        }
    }
}