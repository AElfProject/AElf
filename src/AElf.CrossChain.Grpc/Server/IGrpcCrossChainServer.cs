using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Grpc.Server;

public interface IGrpcCrossChainServer : IDisposable
{
    bool IsStarted { get; }
    Task StartAsync(int listeningPort);
}