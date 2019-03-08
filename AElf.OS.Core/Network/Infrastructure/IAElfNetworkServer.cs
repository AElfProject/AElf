using System;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer : IDisposable
    {
        IPeerPool PeerPool { get; }

        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}