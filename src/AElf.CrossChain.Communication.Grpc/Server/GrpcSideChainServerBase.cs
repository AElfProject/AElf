using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

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

        public override async Task RequestIndexingFromSideChain(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<SideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            var requestedHeight = crossChainRequest.NextHeight;
            while (requestedHeight - crossChainRequest.NextHeight <= CrossChainCommunicationConstants.MaximalIndexingCount)
            {
                var sideChainBlock = await _crossChainResponseService.ResponseSideChainBlockDataAsync(requestedHeight);
                if (sideChainBlock == null)
                    break;
                await responseStream.WriteAsync(sideChainBlock);
                requestedHeight++;
            }
        }
    }
}