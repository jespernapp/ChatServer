using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChatServer
{
    public class Chathub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("RecieveMessage", user, message);
        }
    }
}
