using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcBasicServerBase : BasicCrossChainRpc.BasicCrossChainRpcBase, ITransientDependency
    {
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<GrpcBasicServerBase> Logger { get; set; }
        
        public override Task<HandShakeReply> CrossChainHandShake(HandShake request, ServerCallContext context)
        {
            Logger.LogTrace($"Received shake from chain {ChainHelper.ConvertChainIdToBase58(request.FromChainId)}.");
            _ = PublishCrossChainRequestReceivedEvent(request.Host, request.ListeningPort, request.FromChainId);
            return Task.FromResult(new HandShakeReply {Success = true});
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