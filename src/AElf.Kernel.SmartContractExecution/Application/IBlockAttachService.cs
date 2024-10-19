using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
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

public partial class BlockAttachService : IBlockAttachService, ITransientDependency
{
    private readonly IBlockchainExecutingService _blockchainExecutingService;
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
    private readonly IChainBlockLinkService _chainBlockLinkService;

    public BlockAttachService(IBlockchainService blockchainService,
        IBlockchainExecutingService blockchainExecutingService,
        IChainBlockLinkService chainBlockLinkService,
        IBlockExecutionResultProcessingService blockExecutionResultProcessingService)
    {
        _blockchainService = blockchainService;
        _blockchainExecutingService = blockchainExecutingService;
        _chainBlockLinkService = chainBlockLinkService;
        _blockExecutionResultProcessingService = blockExecutionResultProcessingService;

        Logger = NullLogger<BlockAttachService>.Instance;
    }

    public ILogger<BlockAttachService> Logger { get; set; }

    public async Task AttachBlockAsync(Block block)
    {
        var chain = await _blockchainService.GetChainAsync();

        var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
        if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
        {
            Logger.LogDebug($"Try to attach to chain but the status is {status}.");
            return;
        }

        var notExecutedChainBlockLinks =
            await _chainBlockLinkService.GetNotExecutedChainBlockLinksAsync(chain.LongestChainHash);
        var notExecutedBlocks =
            await _blockchainService.GetBlocksAsync(notExecutedChainBlockLinks.Select(l => l.BlockHash));

        var executionResult = await ExecuteBlocksAsync(notExecutedBlocks);
        await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(BlockAttachService),
        MethodName = nameof(HandleExceptionWhileExecutingBlocks))]
    private async Task<BlockExecutionResult> ExecuteBlocksAsync(IEnumerable<Block> notExecutedBlocks)
    {
        return await _blockchainExecutingService.ExecuteBlocksAsync(notExecutedBlocks);
    }
}