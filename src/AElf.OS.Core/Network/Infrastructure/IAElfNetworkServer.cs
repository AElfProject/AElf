using System;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer : IDisposable
    {
        Task<bool> DialPeerAsync(string ipAddress);
        Task DisconnectPeerAsync(IPeer peer, bool sendDisconnect = false);
        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}