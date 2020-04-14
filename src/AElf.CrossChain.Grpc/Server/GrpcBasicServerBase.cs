using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc.Server
{
    public class GrpcBasicServerBase : BasicCrossChainRpc.BasicCrossChainRpcBase, ITransientDependency
    {
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<GrpcBasicServerBase> Logger { get; set; }

        public override Task<HandShakeReply> CrossChainHandShake(HandShake request, ServerCallContext context)
        {
            Logger.LogDebug($"Received shake from chain {ChainHelper.ConvertChainIdToBase58(request.ChainId)}.");

            if (!GrpcUriHelper.TryParsePrefixedEndpoint(context.Peer, out IPEndPoint peerEndpoint))
                return Task.FromResult(new HandShakeReply
                    {Status = HandShakeReply.Types.HandShakeStatus.InvalidHandshakeRequest});

            _ = PublishCrossChainRequestReceivedEvent(peerEndpoint.Address.ToString(), request.ListeningPort,
                request.ChainId);
            return Task.FromResult(new HandShakeReply {Status = HandShakeReply.Types.HandShakeStatus.Success});
        }

        private Task PublishCrossChainRequestReceivedEvent(string host, int port, int chainId)
        {
            return LocalEventBus.PublishAsync(new NewChainConnectionEvent
            {
                RemoteServerHost = host,
                RemoteServerPort = port,
                RemoteChainId = chainId
            });
        }
    }
}