using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Application;

public partial class CrossChainRequestService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequestingCrossChainData(
        OperationCanceledException ex, KeyValuePair<int, long> chainIdHeightPair)
    {
        var chainIdBased58 = ChainHelper.ConvertChainIdToBase58(chainIdHeightPair.Key);
        Logger.LogWarning(ex, $"Request chain {chainIdBased58} failed.");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue,
        };
    }
}