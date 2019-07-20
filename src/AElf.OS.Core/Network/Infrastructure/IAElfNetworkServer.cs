using System;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer
    {
        Task<bool> ConnectAsync(string ipAddress);
        Task DisconnectAsync(IPeer peer, bool sendDisconnect = false);
        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}