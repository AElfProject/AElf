using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainConnectionEventHandler : ILocalEventHandler<NewChainConnectionEvent>, ITransientDependency
    {
        private readonly IGrpcClientPlugin _grpcClientPlugin;

        public GrpcCrossChainConnectionEventHandler(IGrpcClientPlugin grpcClientPlugin)
        {
            _grpcClientPlugin = grpcClientPlugin;
        }

        public async Task HandleEventAsync(NewChainConnectionEvent eventData)
        {
            _ = _grpcClientPlugin.CreateClientAsync(new CrossChainClientDto
            {
                RemoteChainId = eventData.RemoteChainId,
                RemoteServerHost = eventData.RemoteServerHost,
                RemoteServerPort = eventData.RemoteServerPort
            });
        }
    }
}