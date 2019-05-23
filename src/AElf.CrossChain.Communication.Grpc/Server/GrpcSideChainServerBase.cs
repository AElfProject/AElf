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
    public class GrpcSideChainServerBase : CrossChainRpc.CrossChainRpcBase, ITransientDependency
    {
        public ILogger<GrpcParentChainServerBase> Logger { get; set; }
        private readonly ICrossChainResponseService _crossChainResponseService;

        internal GrpcSideChainServerBase(ICrossChainResponseService crossChainResponseService)
        {
            _crossChainResponseService = crossChainResponseService;
        }

        public override async Task RequestIndexingFromSideChainAsync(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<SideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            var requestedHeight = crossChainRequest.NextHeight;
            while (true)
            {
                var sideChainBlock = await _crossChainResponseService.ResponseSideChainBlockDataAsync(requestedHeight);
                if (sideChainBlock == null)
                    break;
                await responseStream.WriteAsync(sideChainBlock);
                requestedHeight++;
            }
            
//            PublishCrossChainRequestReceivedEvent(context.Host, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }
    }
}