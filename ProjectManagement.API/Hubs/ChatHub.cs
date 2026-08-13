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

        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            // Resolve receiverId (it could be a username, email, or actual database Guid ID)
            var receiver = await _context.Users.FirstOrDefaultAsync(u => 
                u.Id == receiverId || 
                u.UserName == receiverId || 
                u.Email == receiverId
            );

            if (receiver == null)
            {
                Console.WriteLine($"[SIGNALR ERROR]: Receiver '{receiverId}' could not be found in the database users list.");
                return;
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            // 1. Save message to database for history
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // 2. Send message to receiver
            await Clients.User(receiver.Id).SendAsync("ReceiveMessage", senderId, content, message.Timestamp);

            // 3. Send message back to sender (caller) to sync state
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, content, message.Timestamp);
        }
    }
}
