using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChatServer
{
    public class Chathub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        private static Dictionary<string, string> _connections = new();
        
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task RegisterUser(string username)
        {
            _connections[Context.ConnectionId] = username;

            await Clients.All.SendAsync("UpdateUserList", _connections.Values);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.Remove(Context.ConnectionId);
            await Clients.All.SendAsync("UpdateUserList", _connections.Values);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
