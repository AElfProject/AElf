using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Grpc
{
    public interface ICrossChainServer : IDisposable
    {
        Task StartAsync(string localServerIP, int localServerPort);
    }
}