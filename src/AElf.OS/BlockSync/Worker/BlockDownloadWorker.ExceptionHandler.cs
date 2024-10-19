using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel;
using AElf.OS.BlockSync.Types;
using Microsoft.Extensions.Logging;

namespace AElf.OS.BlockSync.Worker;

public partial class BlockDownloadWorker
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhilePerformingDownloading(Exception ex,
        BlockDownloadJobInfo jobInfo, Chain chain)
    {
        await RemoveJobAndTargetStateAsync(jobInfo);
        Logger.LogError(ex, "Handle download job failed.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}