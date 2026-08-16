using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
using System;
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

            // Get sender information
            var sender = await _context.Users.FindAsync(senderId);

            var senderName = sender != null
                ? (sender.NameAr ?? sender.UserName ?? "Unknown")
                : "Unknown";

            var senderPhoto = sender?.ProfilePhoto ?? string.Empty;

            // Company-wide message
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = null,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            // Save message
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Send one object to everyone connected to the chat
            await Clients.All.SendAsync(
                "ReceiveMessage",
                new
                {
                    senderId = senderId,
                    senderName = senderName,
                    senderPhoto = senderPhoto,
                    content = message.Content,
                    timestamp = message.Timestamp
                }
            );
        }
    }
}