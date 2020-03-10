using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockAttachService
    {
        Task AttachBlockAsync(Block block);
    }

    public class BlockAttachService : IBlockAttachService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;

        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger<BlockAttachService> Logger { get; set; }

        public BlockAttachService(IBlockchainService blockchainService,
            IBlockchainExecutingService blockchainExecutingService)
        {
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;

            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<BlockAttachService>.Instance;
        }

        public async Task AttachBlockAsync(Block block)
        {
            var chain = await _blockchainService.GetChainAsync();

            if (chain.BestChainHeight > Constants.GenesisBlockHeight)
            {
                var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
                if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
                {
                    Logger.LogDebug($"Try to attach to chain but the status is {status}.");
                    return;
                }
            }

            var notExecutedBlocks = await _blockchainService.GetNotExecutedBlocksAsync(chain.LongestChainHash);

            BlockExecutionResult executionResult;
            try
            {
                executionResult = await _blockchainExecutingService.ExecuteBlocksAsync(notExecutedBlocks);
            }
            catch (Exception e)
            {
                await _blockchainService.RemoveLongestBranchAsync(chain);
                Logger.LogError(e, "Block execute fails.");
                throw;
            }

            await ProcessExecutionResultAsync(chain, executionResult);
        }

        private async Task ProcessExecutionResultAsync(Chain chain, BlockExecutionResult executionResult)
        {
            if (executionResult.ExecutedFailedBlocks.Any() ||
                executionResult.ExecutedSuccessBlocks.Count == 0 ||
                executionResult.ExecutedSuccessBlocks.Last().Height < chain.BestChainHeight)
            {
                await SetBlockExecutionStatusAsync(executionResult.ExecutedFailedBlocks, false);
                await _blockchainService.RemoveLongestBranchAsync(chain);

                Logger.LogWarning("No block executed successfully or no block is higher than best chain.");
                return;
            }

            var lastExecutedSuccessBlock = executionResult.ExecutedSuccessBlocks.Last();
            await _blockchainService.SetBestChainAsync(chain, lastExecutedSuccessBlock.Height,
                lastExecutedSuccessBlock.GetHash());
            await SetBlockExecutionStatusAsync(executionResult.ExecutedSuccessBlocks, true);
            await PublishBestChainFoundEventAsync(chain, executionResult.ExecutedSuccessBlocks);
            
            Logger.LogInformation(
                $"Attach blocks to best chain, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");
            
        }

        private async Task SetBlockExecutionStatusAsync(IEnumerable<Block> blocks, bool isExecutedSuccess)
        {
            foreach (var block in blocks)
            {
                await _blockchainService.SetBlockExecutionStatusAsync(block.GetHash(), isExecutedSuccess);
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