using System;
using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;
using Grpc.Core;

namespace AElf.CrossChain.Grpc
{
    public interface ICrossChainServer : IDisposable
    {
        Task StartAsync(string localServerIP, int localServerPort, KeyCertificatePair keyCertificatePair);
    }
}