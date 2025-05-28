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
                Context.Abort();
                return;
            }

            ConnectedUsers[Context.ConnectionId] = username;
            await Clients.All.SendAsync("UpdateUserList", ConnectedUsers.Values);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUsers.TryRemove(Context.ConnectionId, out _);

            await Clients.All.SendAsync("UpdateUserList", ConnectedUsers.Values);
            await base.OnDisconnectedAsync(exception);
        }
    }
}