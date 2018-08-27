using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AElf.RPC.Hubs.Net
{
    public class NetworkHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "net");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "net");
            await base.OnDisconnectedAsync(exception);
        }
    }
}