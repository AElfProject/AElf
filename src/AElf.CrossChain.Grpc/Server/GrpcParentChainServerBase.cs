using System;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc.Server
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
            Logger.LogDebug(
                $"Parent Chain Server received IndexedInfo message from chain {ChainHelper.ConvertChainIdToBase58(crossChainRequest.ChainId)}.");
            var requestedHeight = crossChainRequest.NextHeight;
            var remoteChainId = crossChainRequest.ChainId;
            while (requestedHeight - crossChainRequest.NextHeight < GrpcCrossChainConstants.MaximalIndexingCount)
            {
                var parentChainBlockData =
                    await _crossChainResponseService.ResponseParentChainBlockDataAsync(requestedHeight, remoteChainId);
                if (parentChainBlockData == null)
                    break;

                if (context.Status.StatusCode != Status.DefaultSuccess.StatusCode)
                {
                    Logger.LogTrace(
                        $"Disconnected with side chain {ChainHelper.ConvertChainIdToBase58(crossChainRequest.ChainId)} node.");
                    return;
                }

                try
                {
                    await responseStream.WriteAsync(parentChainBlockData);
                    requestedHeight++;
                }
                catch (InvalidOperationException)
                {
                    Logger.LogWarning("Failed to write into server side stream.");
                    return;
                }
            }
        }

        public override async Task<ChainInitializationData> RequestChainInitializationDataFromParentChain(
            SideChainInitializationRequest request, ServerCallContext context)
        {
            Logger.LogDebug(
                $"Received initialization data request from chain {ChainHelper.ConvertChainIdToBase58(request.ChainId)}");
            var sideChainInitializationResponse =
                await _crossChainResponseService.ResponseChainInitializationDataFromParentChainAsync(request.ChainId);
            Logger.LogDebug(
                $"Response initialization data for chain {ChainHelper.ConvertChainIdToBase58(request.ChainId)}");
            return sideChainInitializationResponse;
        }
    }
}