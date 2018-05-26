using System;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network
{
    public interface IAElfServer
    {
        event EventHandler ClientConnected;
        
        Task Start(CancellationToken? token = null);
    }
}