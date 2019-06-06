using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Communication.Grpc
{
    public interface IGrpcCrossChainServer : IDisposable
    {
        Task StartAsync(string localServerHost, int localServerPort);
    }
}