using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutionResultProcessingService : IBlockExecutionResultProcessingService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<BlockExecutionResultProcessingService> Logger { get; set; }

        public BlockExecutionResultProcessingService(IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            _blockchainService = blockchainService;
            _chainBlockLinkService = chainBlockLinkService;

            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<BlockExecutionResultProcessingService>.Instance;
        }

        public async Task ProcessBlockExecutionResultAsync(BlockExecutionResult blockExecutionResult)
        {
            var chain = await _blockchainService.GetChainAsync();

            if (blockExecutionResult.ExecutedFailedBlocks.Any() ||
                blockExecutionResult.ExecutedSuccessBlocks.Count == 0 ||
                blockExecutionResult.ExecutedSuccessBlocks.Last().Height < chain.BestChainHeight)
            {
                await SetBlockExecutionStatusAsync(blockExecutionResult.ExecutedFailedBlocks,
                    ChainBlockLinkExecutionStatus.ExecutionFailed);
                await _blockchainService.RemoveLongestBranchAsync(chain);

                Logger.LogWarning("No block executed successfully or no block is higher than best chain.");
                return;
            }

            var lastExecutedSuccessBlock = blockExecutionResult.ExecutedSuccessBlocks.Last();
            await _blockchainService.SetBestChainAsync(chain, lastExecutedSuccessBlock.Height,
                lastExecutedSuccessBlock.GetHash());
            await SetBlockExecutionStatusAsync(blockExecutionResult.ExecutedSuccessBlocks,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);
            await PublishBestChainFoundEventAsync(chain, blockExecutionResult.ExecutedSuccessBlocks);

            Logger.LogInformation(
                $"Attach blocks to best chain, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");
        }

        private async Task SetBlockExecutionStatusAsync(IEnumerable<Block> blocks, ChainBlockLinkExecutionStatus status)
        {
            foreach (var block in blocks)
            {
                await _chainBlockLinkService.SetChainBlockLinkExecutionStatusAsync(block.GetHash(), status);
            }
        }

        private async Task PublishBestChainFoundEventAsync(Chain chain, List<Block> successBlocks)
        {
            await LocalEventBus.PublishAsync(new BestChainFoundEventData
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight,
                ExecutedBlocks = successBlocks
            });
        }
    }
}