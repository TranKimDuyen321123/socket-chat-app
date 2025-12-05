using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatHub : Hub
    {
        private readonly ChatContext _context;
        
        private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

        public ChatHub(ChatContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var item = _onlineUsers.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(item.Key))
            {
                _onlineUsers.TryRemove(item.Key, out _);
                await Clients.All.SendAsync("UserStatusChanged", item.Key, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterConnection(string username)
        {
            _onlineUsers[username] = Context.ConnectionId;
            await Clients.All.SendAsync("UserStatusChanged", username, true);
            await PushFriendListToClient(username, Context.ConnectionId);
            await LoadMessageHistory(username, "ALL"); 
        }

        public async Task SendFriendRequest(string sender, string targetUser)
        {
            if (sender == targetUser) return;

            var target = await _context.Users.FirstOrDefaultAsync(u => u.Username == targetUser);
            if (target == null) 
            {
                await Clients.Caller.SendAsync("Error", "Người dùng không tồn tại.");
                return;
            }

            var existing = await _context.Friendships.FirstOrDefaultAsync(f => 
                (f.RequesterName == sender && f.ReceiverName == targetUser) || 
                (f.RequesterName == targetUser && f.ReceiverName == sender));

            if (existing != null)
            {
                await Clients.Caller.SendAsync("Error", existing.IsAccepted ? "Hai bạn đã là bạn bè." : "Đã có lời mời kết bạn đang chờ.");
                return;
            }

            _context.Friendships.Add(new Friendship { RequesterName = sender, ReceiverName = targetUser, IsAccepted = false });
            await _context.SaveChangesAsync();

            await Clients.Caller.SendAsync("Success", $"Đã gửi lời mời tới {targetUser}");
            
            if (_onlineUsers.TryGetValue(targetUser, out var connId))
            {
                await Clients.Client(connId).SendAsync("ReceiveFriendRequest", sender);
                await PushFriendListToClient(targetUser, connId);
            }
        }

        public async Task AcceptFriendRequest(string receiver, string requester)
        {
            var friendship = await _context.Friendships.FirstOrDefaultAsync(f => 
                f.RequesterName == requester && f.ReceiverName == receiver && !f.IsAccepted);

            if (friendship == null) return;

            friendship.IsAccepted = true;
            await _context.SaveChangesAsync();

            await PushFriendListToClient(receiver, Context.ConnectionId);
            await Clients.Caller.SendAsync("Success", $"Đã chấp nhận kết bạn với {requester}");

            if (_onlineUsers.TryGetValue(requester, out var connId))
            {
                await Clients.Client(connId).SendAsync("Success", $"{receiver} đã đồng ý kết bạn!");
                await PushFriendListToClient(requester, connId);
            }
        }

        private async Task PushFriendListToClient(string username, string connectionId)
        {
            var friends = await _context.Friendships
                .Where(f => (f.RequesterName == username || f.ReceiverName == username) && f.IsAccepted)
                .Select(f => f.RequesterName == username ? f.ReceiverName : f.RequesterName)
                .ToListAsync();

            var requests = await _context.Friendships
                .Where(f => f.ReceiverName == username && !f.IsAccepted)
                .Select(f => f.RequesterName)
                .ToListAsync();

            var friendListWithStatus = friends.Select(f => new 
            { 
                Username = f, 
                IsOnline = _onlineUsers.ContainsKey(f) 
            }).ToList();

            await Clients.Client(connectionId).SendAsync("UpdateFriendList", friendListWithStatus);
            await Clients.Client(connectionId).SendAsync("UpdateFriendRequests", requests);
        }

        public async Task LoadMessageHistory(string currentUser, string target)
        {
            var query = _context.Messages.AsQueryable();

            if (target == "ALL")
            {
                query = query.Where(m => m.ReceiverName == "ALL" || m.ReceiverName == null);
            }
            else
            {
                query = query.Where(m => (m.SenderName == currentUser && m.ReceiverName == target) || 
                                         (m.SenderName == target && m.ReceiverName == currentUser));
            }

            var history = await query
                .OrderByDescending(m => m.Timestamp).Take(50)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { 
                    m.SenderName, 
                    m.Content, 
                    m.Timestamp, 
                    Type = (int)m.Type, // Cast Enum to int
                    m.AttachmentName 
                })
                .ToListAsync();
            
            await Clients.Caller.SendAsync("LoadHistory", history);
        }

        public async Task SendMessage(string sender, string receiver, string content, int type = 0, string attachmentName = "")
        {
            MessageType msgType = (MessageType)type;

            // Validate friend status if private
            if (receiver != "ALL")
            {
                var isFriend = await _context.Friendships.AnyAsync(f => 
                    ((f.RequesterName == sender && f.ReceiverName == receiver) || 
                     (f.RequesterName == receiver && f.ReceiverName == sender)) && f.IsAccepted);

                if (!isFriend)
                {
                    await Clients.Caller.SendAsync("Error", "Bạn chưa kết bạn với người này.");
                    return;
                }
            }

            var msg = new Message 
            { 
                SenderName = sender, 
                ReceiverName = receiver, 
                Content = content, 
                Type = msgType,
                AttachmentName = attachmentName,
                Timestamp = DateTime.Now 
            };
            
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // Gửi Real-time
            // Gửi về object đầy đủ để Client dễ xử lý
            var msgDto = new { 
                SenderName = sender, 
                Content = content, 
                Type = (int)msgType, 
                AttachmentName = attachmentName 
            };

            if (receiver == "ALL")
            {
                await Clients.All.SendAsync("ReceiveMessage", msgDto);
            }
            else
            {
                if (_onlineUsers.TryGetValue(receiver, out var connId))
                {
                    await Clients.Client(connId).SendAsync("ReceivePrivateMessage", msgDto);
                }
                await Clients.Caller.SendAsync("ReceivePrivateMessage", msgDto);
            }
        }
    }
}
