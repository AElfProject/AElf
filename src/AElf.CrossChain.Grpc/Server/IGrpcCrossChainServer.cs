using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Grpc.Server
{
    public interface IGrpcCrossChainServer : IDisposable
    {
        Task StartAsync(int listeningPort);
        bool IsStarted { get; }
    }
}