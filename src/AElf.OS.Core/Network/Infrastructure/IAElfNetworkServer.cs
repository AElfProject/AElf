using System;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer : IDisposable
    {
        Task StartAsync();
        Task StopAsync(bool gracefulDisconnect = true);
    }
}