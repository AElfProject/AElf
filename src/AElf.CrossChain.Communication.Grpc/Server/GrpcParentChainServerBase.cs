using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcParentChainServerBase : ParentChainRpc.ParentChainRpcBase, ITransientDependency
    {
        public ILogger<GrpcParentChainServerBase> Logger { get; set; }
        private readonly ICrossChainResponseService _crossChainResponseService;

        public GrpcParentChainServerBase(ICrossChainResponseService crossChainResponseService)
        {
            _crossChainResponseService = crossChainResponseService;
        }

        public override async Task RequestIndexingFromParentChain(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<ParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace(
                $"Parent Chain Server received IndexedInfo message from chain {ChainHelper.ConvertChainIdToBase58(crossChainRequest.FromChainId)}.");
            var requestedHeight = crossChainRequest.NextHeight;
            var remoteChainId = crossChainRequest.FromChainId;
            while (requestedHeight - crossChainRequest.NextHeight <= CrossChainCommunicationConstants.MaximalIndexingCount)
            {
                var parentChainBlockData =
                    await _crossChainResponseService.ResponseParentChainBlockDataAsync(requestedHeight, remoteChainId);
                if (parentChainBlockData == null)
                    break;
                await responseStream.WriteAsync(parentChainBlockData);
                requestedHeight++;
            }
        }
        
        public override async Task<ChainInitializationData> RequestChainInitializationDataFromParentChain(SideChainInitializationRequest request, ServerCallContext context)
        {
            Logger.LogTrace(
                $"Received initialization data request from  chain {ChainHelper.ConvertChainIdToBase58(request.ChainId)}");
            var sideChainInitializationResponse =
                await _crossChainResponseService.ResponseChainInitializationDataFromParentChainAsync(request.ChainId);
            return sideChainInitializationResponse;
        }
    }
}