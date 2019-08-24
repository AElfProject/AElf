using System;
using System.Net;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer
    {
        Task<bool> ConnectAsync(IPEndPoint endpoint);
        Task DisconnectAsync(IPeer peer, bool sendDisconnect = false);
        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}