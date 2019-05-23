using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcBasicServerBase : CrossChainRpc.CrossChainRpcBase, ITransientDependency
    {
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<GrpcParentChainServerBase> Logger { get; set; }

        public override Task<HandShakeReply> CrossChainIndexingShakeAsync(HandShake request, ServerCallContext context)
        {
            Logger.LogTrace($"Received shake from chain {ChainHelpers.ConvertChainIdToBase58(request.FromChainId)}.");
            PublishCrossChainRequestReceivedEvent(request.Host, request.ListeningPort, request.FromChainId);
            return Task.FromResult(new HandShakeReply {Result = true});
        }
        
        private void PublishCrossChainRequestReceivedEvent(string host, int port, int chainId)
        {
            LocalEventBus.PublishAsync(new GrpcCrossChainRequestReceivedEvent
            {
                RemoteServerHost = host,
                RemoteServerPort = port,
                RemoteChainId = chainId
            });
        }
    }
}