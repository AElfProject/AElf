using System;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network
{
    public interface IAElfServer : IDisposable
    {
        event EventHandler ClientConnected;
        
        Task StartAsync(CancellationToken? token = null);
    }
}