using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Data;
using ProjectManagement.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.API.Hubs
{
    public static class ChatConnectionManager
    {
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.HashSet<string>> OnlineUsers 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.HashSet<string>>();

        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> LastSeenTimes 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();

        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> LastActivityTimes 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
    }

    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                var connectionId = Context.ConnectionId;
                
                ChatConnectionManager.OnlineUsers.AddOrUpdate(
                    userId,
                    id => new HashSet<string> { connectionId },
                    (id, set) =>
                    {
                        lock (set)
                        {
                            set.Add(connectionId);
                        }
                        return set;
                    }
                );

                ChatConnectionManager.LastActivityTimes[userId] = DateTime.UtcNow;
                ChatConnectionManager.LastSeenTimes.TryRemove(userId, out _);

                bool isNewlyOnline = false;
                if (ChatConnectionManager.OnlineUsers.TryGetValue(userId, out var set))
                {
                    lock (set)
                    {
                        isNewlyOnline = (set.Count == 1);
                    }
                }

                if (isNewlyOnline)
                {
                    await Clients.Others.SendAsync("UserStatusChanged", userId, true);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                var connectionId = Context.ConnectionId;
                bool isNewlyOffline = false;

                if (ChatConnectionManager.OnlineUsers.TryGetValue(userId, out var set))
                {
                    lock (set)
                    {
                        set.Remove(connectionId);
                        isNewlyOffline = (set.Count == 0);
                    }

                    if (isNewlyOffline)
                    {
                        ChatConnectionManager.OnlineUsers.TryRemove(userId, out _);
                        ChatConnectionManager.LastSeenTimes[userId] = DateTime.UtcNow;
                    }
                }

                if (isNewlyOffline)
                {
                    await Clients.Others.SendAsync("UserStatusChanged", userId, false);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string content, int? replyToMessageId)
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId) ||
                string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            ChatConnectionManager.LastActivityTimes[senderId] = DateTime.UtcNow;

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
                ReplyToMessageId = replyToMessageId,
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

            ChatConnectionManager.LastActivityTimes[senderId] = DateTime.UtcNow;

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

            ChatConnectionManager.LastActivityTimes[senderId] = DateTime.UtcNow;

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

        public async Task SendReaction(int messageId, string emoji)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(emoji))
                return;

            ChatConnectionManager.LastActivityTimes[userId] = DateTime.UtcNow;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return;

            var userName = user.NameAr ?? user.UserName ?? "Unknown";

            var messageExists = await _context.ChatMessages.AnyAsync(m => m.Id == messageId);
            if (!messageExists)
                return;

            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

            bool isRemoved = false;

            if (existingReaction != null)
            {
                if (existingReaction.Emoji == emoji)
                {
                    _context.MessageReactions.Remove(existingReaction);
                    isRemoved = true;
                }
                else
                {
                    existingReaction.Emoji = emoji;
                    existingReaction.Timestamp = DateTime.UtcNow;
                }
            }
            else
            {
                var newReaction = new MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    Emoji = emoji,
                    Timestamp = DateTime.UtcNow
                };
                _context.MessageReactions.Add(newReaction);
            }

            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveReaction", new
            {
                messageId = messageId,
                userId = userId,
                userName = userName,
                emoji = emoji,
                isRemoved = isRemoved
            });
        }
        public async Task MarkMessageAsRead(int messageId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            var messageExists = await _context.ChatMessages
                .AnyAsync(m => m.Id == messageId);

            if (!messageExists)
                return;

            var alreadyRead = await _context.MessageReadStates
                .AnyAsync(r =>
                    r.MessageId == messageId &&
                    r.UserId == userId);

            if (alreadyRead)
                return;

            var readAt = DateTime.UtcNow;

            var readState = new MessageReadState
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = readAt
            };

            _context.MessageReadStates.Add(readState);

            await _context.SaveChangesAsync();

            await Clients.All.SendAsync(
                "MessageRead",
                new
                {
                    messageId = messageId,
                    userId = userId,
                    readAt = readAt
                }
            );
        }
    }
}