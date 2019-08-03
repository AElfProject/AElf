using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainPlugin : INodePlugin, ILocalEventHandler<NewChainConnectionEvent>, ITransientDependency
    {
        private readonly IGrpcClientPlugin _grpcClientPlugin;
        private readonly IGrpcServePlugin _grpcServePlugin;

        public GrpcCrossChainPlugin(IGrpcClientPlugin grpcClientPlugin, IGrpcServePlugin grpcServePlugin)
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

        private void CreateClient(CrossChainClientDto grpcCrossChainClientDto)
        {
            _grpcClientPlugin.CreateClientAsync(grpcCrossChainClientDto);
        }

        public Task HandleEventAsync(NewChainConnectionEvent eventData)
        {
            CreateClient(new CrossChainClientDto
            {
                RemoteChainId = eventData.RemoteChainId,
                RemoteServerHost = eventData.RemoteServerHost,
                RemoteServerPort = eventData.RemoteServerPort
            });
            return Task.CompletedTask;
        }
    }
}