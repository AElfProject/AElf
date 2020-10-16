using System.Threading.Tasks;
using AElf.CrossChain.Communication;
using AElf.CrossChain.Grpc.Client;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainConnectionEventHandler : ILocalEventHandler<NewChainConnectionEvent>, ITransientDependency
    {
        private readonly IGrpcClientPlugin _grpcClientPlugin;
        private readonly ICrossChainCommunicationPlugin _crossChainCommunicationPlugin;

        public GrpcCrossChainConnectionEventHandler(IGrpcClientPlugin grpcClientPlugin, ICrossChainCommunicationPlugin crossChainCommunicationPlugin)
        {
            _grpcClientPlugin = grpcClientPlugin;
            _crossChainCommunicationPlugin = crossChainCommunicationPlugin;
        }

        public Task HandleEventAsync(NewChainConnectionEvent eventData)
        {
            return _grpcClientPlugin.CreateClientAsync(new GrpcCrossChainClientCreationContext
            {
                RemoteChainId = eventData.RemoteChainId,
                RemoteServerHost = eventData.RemoteServerHost,
                RemoteServerPort = eventData.RemoteServerPort,
                LocalChainId = _crossChainCommunicationPlugin.ChainId
            });
        }
    }
}