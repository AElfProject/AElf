using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcNodePlugin : INodePlugin
    {
        private readonly IGrpcClientPlugin _grpcClientPlugin;
        private readonly IGrpcServePlugin _grpcServePlugin;

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
            await _grpcClientPlugin.StopAsync();
            await _grpcServePlugin.StopAsync();
        }
    }
}