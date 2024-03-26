using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application;

public interface IBlockAttachService
{
    Task AttachBlockAsync(Block block);
}

public class BlockAttachService : IBlockAttachService, ITransientDependency
{
    private readonly IBlockchainExecutingService _blockchainExecutingService;
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
    private readonly IChainBlockLinkService _chainBlockLinkService;
    private readonly ActivitySource _activitySource;

    public BlockAttachService(IBlockchainService blockchainService,
        IBlockchainExecutingService blockchainExecutingService,
        IChainBlockLinkService chainBlockLinkService,
        IBlockExecutionResultProcessingService blockExecutionResultProcessingService,
        Instrumentation instrumentation)
    {
        _blockchainService = blockchainService;
        _blockchainExecutingService = blockchainExecutingService;
        _chainBlockLinkService = chainBlockLinkService;
        _blockExecutionResultProcessingService = blockExecutionResultProcessingService;

        Logger = NullLogger<BlockAttachService>.Instance;
        _activitySource = instrumentation.ActivitySource;
    }

    public ILogger<BlockAttachService> Logger { get; set; }

    public async Task AttachBlockAsync(Block block)
    {
        using var activity = _activitySource.StartActivity();
        var stopwatch = Stopwatch.StartNew();
        var chain = await _blockchainService.GetChainAsync();
        stopwatch.Stop();
        Logger.LogDebug("GetChainAsync time{Time} ",
            stopwatch.ElapsedMilliseconds);
        stopwatch.Start();
        var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
        stopwatch.Stop();
        Logger.LogDebug("AttachBlockToChainAsync time{Time} ",
            stopwatch.ElapsedMilliseconds);
        if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
        {
            Logger.LogDebug($"Try to attach to chain but the status is {status}.");
            return;
        }

        stopwatch.Start();
        var notExecutedChainBlockLinks =
            await _chainBlockLinkService.GetNotExecutedChainBlockLinksAsync(chain.LongestChainHash);
        var notExecutedBlocks =
            await _blockchainService.GetBlocksAsync(notExecutedChainBlockLinks.Select(l => l.BlockHash));
        stopwatch.Stop();
        Logger.LogDebug("GetNotExecutedChainBlockLinksAsync time{Time} ",
            stopwatch.ElapsedMilliseconds);
        var executionResult = new BlockExecutionResult();
        try
        {
            stopwatch.Start();
            executionResult = await _blockchainExecutingService.ExecuteBlocksAsync(notExecutedBlocks);
            stopwatch.Stop();
            Logger.LogDebug("blockchainExecutingService.ExecuteBlocksAsync time{Time} ",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Block execute fails.");
            throw;
        }
        finally
        {
            stopwatch.Start();
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);
            stopwatch.Stop();
            Logger.LogDebug("ProcessBlockExecutionResultAsync time{Time} ",
                stopwatch.ElapsedMilliseconds);
        }
    }
}