using System;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcSideChainServerBase : SideChainRpc.SideChainRpcBase, ITransientDependency
    {
        public ILogger<GrpcSideChainServerBase> Logger { get; set; }
        private readonly ICrossChainResponseService _crossChainResponseService;

        public GrpcSideChainServerBase(ICrossChainResponseService crossChainResponseService)
        {
            _crossChainResponseService = crossChainResponseService;
        }

        public override async Task RequestIndexingFromSideChainAsync(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<SideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            var requestedHeight = crossChainRequest.NextHeight;
            while (requestedHeight - crossChainRequest.NextHeight <= 64)
            {
                var sideChainBlock = await _crossChainResponseService.ResponseSideChainBlockDataAsync(requestedHeight);
                if (sideChainBlock == null)
                    break;
                await responseStream.WriteAsync(sideChainBlock);
                requestedHeight++;
            }
            
//            PublishCrossChainRequestReceivedEvent(context.Host, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }
        
//        public override Task<HandShakeReply> CrossChainHandShake(HandShake request, ServerCallContext context)
//        {
//            Logger.LogTrace($"Received shake from chain {ChainHelpers.ConvertChainIdToBase58(request.FromChainId)}.");
////            PublishCrossChainRequestReceivedEvent(context.Host, request.ListeningPort, request.FromChainId);
//            return Task.FromResult(new HandShakeReply {Result = true});
//        }
    }
}