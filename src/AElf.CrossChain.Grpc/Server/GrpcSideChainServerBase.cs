using System;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc.Server
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
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");
            var requestedHeight = crossChainRequest.NextHeight;
            while (requestedHeight - crossChainRequest.NextHeight < GrpcCrossChainConstants.MaximalIndexingCount)
            {
                var sideChainBlock = await _crossChainResponseService.ResponseSideChainBlockDataAsync(requestedHeight);
                if (sideChainBlock == null)
                    break;
                try
                {
                    await responseStream.WriteAsync(sideChainBlock);
                    requestedHeight++;
                }
                catch (InvalidOperationException)
                {
                    Logger.LogWarning("Failed to write into server side stream.");
                    return;
                }
            }
        }
    }
}