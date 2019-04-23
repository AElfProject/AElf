using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainServerNodePlugin : INodePlugin
    {
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly ICrossChainServer _crossChainServer;

        public GrpcCrossChainServerNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            ICrossChainServer crossChainServer)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainServer = crossChainServer;
        }

        public Task StartAsync(int chainId)
        {
            if (!_grpcCrossChainConfigOption.LocalServer)
                return Task.CompletedTask;
            return _crossChainServer.StartAsync(_grpcCrossChainConfigOption.LocalServerIP,
                _grpcCrossChainConfigOption.LocalServerPort);
        }

        public Task ShutdownAsync()
        {
            _crossChainServer.Dispose();
            return Task.CompletedTask;
        }
    }
}