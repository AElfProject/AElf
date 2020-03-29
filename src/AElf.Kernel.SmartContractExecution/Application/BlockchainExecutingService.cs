using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class FullBlockchainExecutingService : IBlockchainExecutingService, ITransientDependency
    {
        //TODO: should only use IBlockchainService
        private readonly IChainManager _chainManager;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly ITransactionResultService _transactionResultService;
        public ILocalEventBus LocalEventBus { get; set; }

        public FullBlockchainExecutingService(IChainManager chainManager,
            IBlockchainService blockchainService, IBlockValidationService blockValidationService,
            IBlockExecutingService blockExecutingService,
            ITransactionResultService transactionResultService, IBlockStateSetManger blockStateSetManger)
        {
            _chainManager = chainManager;
            _blockchainService = blockchainService;
            _blockValidationService = blockValidationService;
            _blockExecutingService = blockExecutingService;
            _transactionResultService = transactionResultService;
            _blockStateSetManger = blockStateSetManger;

            LocalEventBus = NullLocalEventBus.Instance;
        }

        public ILogger<FullBlockchainExecutingService> Logger { get; set; }

        private async Task<bool> TryExecuteBlockAsync(Block block)
        {
            var blockHash = block.GetHash();

            var blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
            if (blockState != null)
                return true;

            var transactions = await _blockchainService.GetTransactionsAsync(block.TransactionIds);
            var BlockExecutedSet = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;

            var blockHashWithoutCache = BlockExecutedSet.GetHashWithoutCache();

            if (blockHashWithoutCache != blockHash)
            {
                blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHashWithoutCache);
                Logger.LogWarning($"Block execution failed. BlockStateSet: {blockState}");
                Logger.LogWarning(
                    $"Block execution failed. Block header: {BlockExecutedSet.Header}, Block body: {BlockExecutedSet.Body}");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Processing pipeline for a block contains ValidateBlockBeforeExecute, ExecuteBlock and ValidateBlockAfterExecute.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>Block processing result is true if succeed, otherwise false.</returns>
        private async Task<bool> TryProcessBlockAsync(Block block)
        {
            var blockHash = block.GetHash();
            // Set the other blocks as bad block if found the first bad block
            if (!await _blockValidationService.ValidateBlockBeforeExecuteAsync(block))
            {
                Logger.LogWarning($"Block validate fails before execution. block hash : {blockHash}");
                return false;
            }

            if (!await TryExecuteBlockAsync(block))
            {
                Logger.LogWarning($"Block execution failed. block hash : {blockHash}");
                return false;
            }

            if (!await _blockValidationService.ValidateBlockAfterExecuteAsync(block))
            {
                Logger.LogWarning($"Block validate fails after execution. block hash : {blockHash}");
                return false;
            }

            //await _transactionResultService.ProcessTransactionResultAfterExecutionAsync(block.Header,
            //    block.Body.TransactionIds.ToList());

            return true;
        }

        private async Task SetBestChainAsync(List<ChainBlockLink> successLinks, Chain chain)
        {
            if (successLinks.Count == 0)
                return;

            Logger.LogDebug($"Set best chain for block height {string.Join(",", successLinks.Select(l => l.Height))}");
            var blockLink = successLinks.Last();
            await _blockchainService.SetBestChainAsync(chain, blockLink.Height, blockLink.BlockHash);
        }

        public async Task<List<ChainBlockLink>> ExecuteBlocksAttachedToLongestChain(Chain chain,
            BlockAttachOperationStatus status)
        {
            //TODO: split the logic of getting blocks to execute, and the logic of executing blocks, and the logic of mark blocks executed
            //only keep the logic of executing blocks here

            if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
            {
                Logger.LogDebug($"Try to attach to chain but the status is {status}.");
                return null;
            }

            var successLinks = new List<ChainBlockLink>();
            var successBlocks = new List<Block>();
            var blockLinks = await _chainManager.GetNotExecutedBlocks(chain.LongestChainHash);

            try
            {
                foreach (var blockLink in blockLinks)
                {
                    var linkedBlock = await _blockchainService.GetBlockByHashAsync(blockLink.BlockHash);

                    var processResult = await TryProcessBlockAsync(linkedBlock);
                    if (!processResult)
                    {
                        await _chainManager.SetChainBlockLinkExecutionStatusAsync(blockLink,
                            ChainBlockLinkExecutionStatus.ExecutionFailed);
                        await _chainManager.RemoveLongestBranchAsync(chain);
                        return null;
                    }

                    successLinks.Add(blockLink);
                    successBlocks.Add(linkedBlock);
                    Logger.LogInformation(
                        $"Executed block {blockLink.BlockHash} at height {blockLink.Height}, with {linkedBlock.Body.TransactionsCount} txns.");

                    await LocalEventBus.PublishAsync(new BlockAcceptedEvent {Block = linkedBlock});
                }
            }
            catch (BlockValidationException ex)
            {
                if (!(ex.InnerException is ValidateNextTimeBlockValidationException))
                {
                    await _chainManager.RemoveLongestBranchAsync(chain);
                    throw;
                }

                Logger.LogWarning(
                    $"Block validation failed: {ex.Message}. Inner exception {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                await _chainManager.RemoveLongestBranchAsync(chain);
                Logger.LogError(ex, "Block validate or execute fails.");
                throw;
            }

            if (successLinks.Count == 0 || successLinks.Last().Height < chain.BestChainHeight)
            {
                Logger.LogWarning("No block execution succeed or no block is higher than best chain.");
                await _chainManager.RemoveLongestBranchAsync(chain);
                return null;
            }

            await SetBestChainAsync(successLinks, chain);
            await _chainManager.SetChainBlockLinkExecutionStatusesAsync(successLinks,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);
            await PublishBestChainFoundEventAsync(chain, successBlocks);

            Logger.LogInformation(
                $"Attach blocks to best chain, status: {status}, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");

            return blockLinks;
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