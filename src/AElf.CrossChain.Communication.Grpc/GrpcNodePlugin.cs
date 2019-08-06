using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcNodePlugin : INodePlugin
    {
        private readonly IEnumerable<IGrpcCrossChainPlugin> _grpcCrossChainPlugins;
        
        public GrpcNodePlugin(IEnumerable<IGrpcCrossChainPlugin> grpcCrossChainPlugins)
        {
            _grpcCrossChainPlugins = grpcCrossChainPlugins;
        }

        public async Task StartAsync(int chainId)
        {
            foreach (var grpcCrossChainPlugin in _grpcCrossChainPlugins)
            {
                await grpcCrossChainPlugin.StartAsync(chainId);
            }
        }
        
        public async Task ShutdownAsync()
        {
            foreach (var grpcCrossChainPlugin in _grpcCrossChainPlugins)
            {
                await grpcCrossChainPlugin.ShutdownAsync();
            }
        }
    }
}