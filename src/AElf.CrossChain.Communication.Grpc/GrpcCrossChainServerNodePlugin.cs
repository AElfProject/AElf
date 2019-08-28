using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainServerNodePlugin : IGrpcServePlugin
    {
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly IGrpcCrossChainServer _grpcCrossChainServer;

        public GrpcCrossChainServerNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IGrpcCrossChainServer grpcCrossChainServer)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _grpcCrossChainServer = grpcCrossChainServer;
        }

        public Task StartAsync(int chainId)
        {
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.ListeningHost) 
                || _grpcCrossChainConfigOption.LocalServerPort == 0)
                return Task.CompletedTask;
            return _grpcCrossChainServer.StartAsync(_grpcCrossChainConfigOption.ListeningHost,
                _grpcCrossChainConfigOption.LocalServerPort);
        }

        public Task ShutdownAsync()
        {
            _grpcCrossChainServer.Dispose();
            return Task.CompletedTask;
        }
    }
}