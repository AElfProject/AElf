using System;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer : IDisposable
    {
        Task<bool> DialPeerAsync(string ipAddress);
        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}