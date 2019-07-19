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
    public class FullBlockchainExecutingService : IBlockchainExecutingService, ISingletonDependency
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        public ILocalEventBus LocalEventBus { get; set; }

        public FullBlockchainExecutingService(IChainManager chainManager,
            IBlockchainService blockchainService, IBlockValidationService blockValidationService,
            IBlockExecutingService blockExecutingService, IBlockchainStateManager blockchainStateManager)
        {
            _chainManager = chainManager;
            _blockchainService = blockchainService;
            _blockValidationService = blockValidationService;
            _blockExecutingService = blockExecutingService;
            _blockchainStateManager = blockchainStateManager;

            LocalEventBus = NullLocalEventBus.Instance;
        }

        public ILogger<FullBlockchainExecutingService> Logger { get; set; }

        private async Task<bool> TryExecuteBlockAsync(Block block)
        {
            var blockHash = block.GetHash();

            var blockState = await _blockchainStateManager.GetBlockStateSetAsync(blockHash);
            if (blockState != null)
                return true;

            var transactions = await _blockchainService.GetTransactionsAsync(block.TransactionIds);
            var executedBlock = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);

            return executedBlock.GetHashWithoutCache().Equals(blockHash);
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

            return true;
        }

        private async Task SetBestChainAsync(List<ChainBlockLink> successLinks, Chain chain)
        {
            if (successLinks.Count == 0)
                return;

            Logger.LogTrace($"Set best chain for block height {string.Join(",", successLinks.Select(l => l.Height))}");
            var blockLink = successLinks.Last();
            await _blockchainService.SetBestChainAsync(chain, blockLink.Height, blockLink.BlockHash);
            await LocalEventBus.PublishAsync(new BestChainFoundEventData
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight,
                ExecutedBlocks = successLinks.Select(p => p.BlockHash).ToList()
            });
        }
        
        public async Task<List<ChainBlockLink>> ExecuteBlocksAttachedToLongestChain(Chain chain,
            BlockAttachOperationStatus status)
        {
            if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
            {
                Logger.LogTrace($"Try to attach to chain but the status is {status}.");
                return null;
            }

            var successLinks = new List<ChainBlockLink>();
            var blockLinks = await _chainManager.GetNotExecutedBlocks(chain.LongestChainHash);

            try
            {
                foreach (var blockLink in blockLinks)
                {
                    var linkedBlock = await _blockchainService.GetBlockByHashAsync(blockLink.BlockHash);

                    var processResult = await TryProcessBlockAsync(linkedBlock);
                    if (!processResult)
                    {
                        await _chainManager.SetChainBlockLinkExecutionStatus(blockLink,
                            ChainBlockLinkExecutionStatus.ExecutionFailed);
                        await _chainManager.RemoveLongestBranchAsync(chain);
                        return null;
                    }
                    
                    await _chainManager.SetChainBlockLinkExecutionStatus(blockLink, 
                        ChainBlockLinkExecutionStatus.ExecutionSuccess);
                    successLinks.Add(blockLink);
                    Logger.LogInformation($"Executed block {blockLink.BlockHash} at height {blockLink.Height}.");
                    await LocalEventBus.PublishAsync(new BlockAcceptedEvent()
                    {
                        BlockHeader = linkedBlock.Header
                    });
                }
            }
            catch (BlockValidationException ex)
            {
                if (!(ex.InnerException is ValidateNextTimeBlockValidationException) || successLinks.Count == 0)
                {
                    await _chainManager.RemoveLongestBranchAsync(chain);
                    throw;
                }
                
                Logger.LogWarning($"Block validation failed: {ex.Message}.");
            }
            catch (Exception ex)
            {
                await _chainManager.RemoveLongestBranchAsync(chain);
                Logger.LogWarning($"Block validate or execute fails. Exception message {ex.Message}");
                throw;
            }

            await SetBestChainAsync(successLinks, chain);
            
            Logger.LogInformation(
                $"Attach blocks to best chain, status: {status}, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");

            return blockLinks;
        }
    }
}