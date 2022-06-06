using System;
using System.Threading.Tasks;
using AElf.CrossChain.Application;
using AElf.Standards.ACS7;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc.Server;

public class GrpcSideChainServerBase : SideChainRpc.SideChainRpcBase, ITransientDependency
{
    private readonly ICrossChainResponseService _crossChainResponseService;

    public GrpcSideChainServerBase(ICrossChainResponseService crossChainResponseService)
    {
        _crossChainResponseService = crossChainResponseService;
    }

    public ILogger<GrpcSideChainServerBase> Logger { get; set; }

    public override async Task RequestIndexingFromSideChain(CrossChainRequest crossChainRequest,
        IServerStreamWriter<SideChainBlockData> responseStream, ServerCallContext context)
    {
        Logger.LogTrace("Side Chain Server received IndexedInfo message.");
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