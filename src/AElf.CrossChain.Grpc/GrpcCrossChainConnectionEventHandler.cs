using System.Threading.Tasks;
using AElf.CrossChain.Grpc.Client;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainConnectionEventHandler : ILocalEventHandler<NewChainConnectionEvent>, ITransientDependency
    {
        private readonly IGrpcClientPlugin _grpcClientPlugin;

        public GrpcCrossChainConnectionEventHandler(IGrpcClientPlugin grpcClientPlugin)
        {
            _grpcClientPlugin = grpcClientPlugin;
        }

        public Task HandleEventAsync(NewChainConnectionEvent eventData)
        {
            return _grpcClientPlugin.CreateClientAsync(new GrpcCrossChainClientCreationContext
            {
                RemoteChainId = eventData.RemoteChainId,
                RemoteServerHost = eventData.RemoteServerHost,
                RemoteServerPort = eventData.RemoteServerPort
            });
        }
    }
}