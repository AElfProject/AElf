using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Events;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application;

public class BlockExecutionResultProcessingService : IBlockExecutionResultProcessingService, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IChainBlockLinkService _chainBlockLinkService;

    public BlockExecutionResultProcessingService(IBlockchainService blockchainService,
        IChainBlockLinkService chainBlockLinkService)
    {
        _blockchainService = blockchainService;
        _chainBlockLinkService = chainBlockLinkService;

        LocalEventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<BlockExecutionResultProcessingService>.Instance;
    }

    public ILocalEventBus LocalEventBus { get; set; }
    public ILogger<BlockExecutionResultProcessingService> Logger { get; set; }

    public async Task ProcessBlockExecutionResultAsync(Chain chain, BlockExecutionResult blockExecutionResult)
    {
        if (blockExecutionResult.ExecutedFailedBlocks.Any() ||
            blockExecutionResult.SuccessBlockExecutedSets.Count == 0 ||
            blockExecutionResult.SuccessBlockExecutedSets.Last().Height < chain.BestChainHeight)
        {
            await SetBlockExecutionStatusAsync(blockExecutionResult.ExecutedFailedBlocks.Select(b => b.GetHash()),
                ChainBlockLinkExecutionStatus.ExecutionFailed);
            await _blockchainService.RemoveLongestBranchAsync(chain);

            Logger.LogDebug("No block executed successfully or no block is higher than best chain.");
            return;
        }

        var lastExecutedSuccessBlock = blockExecutionResult.SuccessBlockExecutedSets.Last();
        await _blockchainService.SetBestChainAsync(chain, lastExecutedSuccessBlock.Height,
            lastExecutedSuccessBlock.GetHash());
        await SetBlockExecutionStatusAsync(blockExecutionResult.SuccessBlockExecutedSets.Select(b => b.GetHash()),
            ChainBlockLinkExecutionStatus.ExecutionSuccess);
        await LocalEventBus.PublishAsync(new BlocksExecutionSucceededEvent
        {
            BlockExecutedSets = blockExecutionResult.SuccessBlockExecutedSets
        });

        // Logger.LogInformation(
            // $"Attach blocks to best chain, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");
    }

    private async Task SetBlockExecutionStatusAsync(IEnumerable<Hash> blockHashes,
        ChainBlockLinkExecutionStatus status)
    {
        foreach (var blockHash in blockHashes)
            await _chainBlockLinkService.SetChainBlockLinkExecutionStatusAsync(blockHash, status);
    }
}