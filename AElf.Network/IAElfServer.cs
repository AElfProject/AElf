using System;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Network
{
    public interface IAElfServer
    {
        event EventHandler ClientConnected;
        
        Task StartAsync(CancellationToken? token = null);
    }
}