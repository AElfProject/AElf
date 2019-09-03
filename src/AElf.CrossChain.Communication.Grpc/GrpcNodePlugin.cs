using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcNodePlugin : INodePlugin
    {
        private readonly IGrpcServePlugin _grpcServePlugin;
        private readonly IGrpcClientPlugin _grpcClientPlugin;
        
        public GrpcNodePlugin(IGrpcClientPlugin grpcClientPlugin, IGrpcServePlugin grpcServePlugin)
        {
            _grpcClientPlugin = grpcClientPlugin;
            _grpcServePlugin = grpcServePlugin;
        }

        public async Task StartAsync(int chainId)
        {
            await _grpcServePlugin.StartAsync(chainId);
            await _grpcClientPlugin.StartAsync(chainId);
        }
        
        public async Task ShutdownAsync()
        {
            await _grpcServePlugin.ShutdownAsync();
            await _grpcClientPlugin.ShutdownAsync();
        }
    }
}