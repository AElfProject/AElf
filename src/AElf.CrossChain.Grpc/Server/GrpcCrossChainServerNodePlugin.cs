using System.Threading.Tasks;
using AElf.CrossChain.Communication;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc.Server
{
    public class GrpcCrossChainServerNodePlugin : ICrossChainCommunicationPlugin
    {
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly IGrpcCrossChainServer _grpcCrossChainServer;
        public int ChainId { get; private set; }

        public GrpcCrossChainServerNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IGrpcCrossChainServer grpcCrossChainServer)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _grpcCrossChainServer = grpcCrossChainServer;
        }

        public Task StartAsync(int chainId)
        {
            ChainId = chainId;

            if (_grpcCrossChainConfigOption.ListeningPort == 0)
                return Task.CompletedTask;
            return _grpcCrossChainServer.StartAsync(_grpcCrossChainConfigOption.ListeningPort);
        }

        public Task ShutdownAsync()
        {
            _grpcCrossChainServer.Dispose();
            return Task.CompletedTask;
        }
    }
}