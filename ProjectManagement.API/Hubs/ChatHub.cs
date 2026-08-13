using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
using System;
using System.Security.Claims;
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

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = null,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            // Save company-wide message
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Send to EVERYONE connected to the ChatHub
            await Clients.All.SendAsync(
                "ReceiveMessage",
                senderId,
                content.Trim(),
                message.Timestamp
            );
        }
    }
}