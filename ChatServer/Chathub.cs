using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ChatServer
{
    public class Chathub : Hub
    {
        // Trådsäker dictionary
        private static ConcurrentDictionary<string, string> ConnectedUsers = new();

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task RegisterUser(string username)
        {
            if (ConnectedUsers.Values.Contains(username))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "Server", $"❌ Username '{username} already taken.");
                return;
            }

            ConnectedUsers[Context.ConnectionId] = username;
            await Clients.All.SendAsync("ReceiveMessage", "Server", $"{username} has joined the chat.");
            await Clients.All.SendAsync("UpdateUserList", ConnectedUsers.Values);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedUsers.TryRemove(Context.ConnectionId, out var username))
            {
                await Clients.All.SendAsync("ReceiveMessage", "Server", $"{username} has left the chat.");
                await Clients.All.SendAsync("UpdateUserList", ConnectedUsers.Values);
            }
            await base.OnDisconnectedAsync(exception);
        }

        //usertyping 
        public async Task UserTyping(string username)
        {
            await Clients.Others.SendAsync("UserTyping", username);
        }

        public async Task UserStoppedTyping(string username)
        {
            await Clients.Others.SendAsync("UserStoppedTyping", username);
        }



    }
}