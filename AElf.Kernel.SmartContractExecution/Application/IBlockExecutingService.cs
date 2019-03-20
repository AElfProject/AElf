using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockExecutingService
    {
        Task<Block> ExecuteBlockAsync(BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions);

        Task<Block> ExecuteBlockAsync(BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken);
    }

    public interface IBlockchainExecutingService
    {
        Task<List<ChainBlockLink>> ExecuteBlocksAttachedToLongestChain(Chain chain, BlockAttachOperationStatus status);
    }

    public class FullBlockchainExecutingService : IBlockchainExecutingService
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutingService _blockExecutingService;
        public ILocalEventBus LocalEventBus { get; set; }

        public FullBlockchainExecutingService(IChainManager chainManager,
            IBlockchainService blockchainService, IBlockValidationService blockValidationService,
            IBlockExecutingService blockExecutingService)
        {
            _chainManager = chainManager;
            _blockchainService = blockchainService;
            _blockValidationService = blockValidationService;
            _blockExecutingService = blockExecutingService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public ILogger<FullBlockchainExecutingService> Logger { get; set; }

        private async Task<bool> ExecuteBlock(ChainBlockLink blockLink, Block block)
        {
            var result = await _blockExecutingService.ExecuteBlockAsync(block.Header, block.Body.TransactionList);
            return result.GetHash().Equals(block.GetHash());
        }

        public async Task<List<ChainBlockLink>> ExecuteBlocksAttachedToLongestChain(Chain chain, BlockAttachOperationStatus status)
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

                    // Set the other blocks as bad block if found the first bad block
                    if (!await _blockValidationService.ValidateBlockBeforeExecuteAsync(linkedBlock))
                    {
                        await _chainManager.SetChainBlockLinkExecutionStatus(blockLink, ChainBlockLinkExecutionStatus.ExecutionFailed);
                        Logger.LogWarning($"Block validate fails before execution. block hash : {blockLink.BlockHash}");
                        break;
                    }

                    if (!await ExecuteBlock(blockLink, linkedBlock))
                    {
                        await _chainManager.SetChainBlockLinkExecutionStatus(blockLink, ChainBlockLinkExecutionStatus.ExecutionFailed);
                        Logger.LogWarning($"Block execution failed. block hash : {blockLink.BlockHash}");
                        break;
                    }

                    if (!await _blockValidationService.ValidateBlockAfterExecuteAsync(linkedBlock))
                    {
                        await _chainManager.SetChainBlockLinkExecutionStatus(blockLink, ChainBlockLinkExecutionStatus.ExecutionFailed);
                        Logger.LogWarning($"Block validate fails after execution. block hash : {blockLink.BlockHash}");
                        break;
                    }

                    await _chainManager.SetChainBlockLinkExecutionStatus(blockLink, ChainBlockLinkExecutionStatus.ExecutionSuccess);

                    successLinks.Add(blockLink);

                    Logger.LogInformation($"Executed block {blockLink.BlockHash} at height {blockLink.Height}.");

                    await LocalEventBus.PublishAsync(new BlockAcceptedEvent()
                    {
                        BlockHeader = linkedBlock.Header
                    });
                }
            }
            catch (ValidateNextTimeBlockValidationException ex)
            {
                Logger.LogWarning($"Block validate fails after execution. block hash : {ex.BlockHash.ToHex()}");
            }

            if (successLinks.Count > 0)
            {
                var blockLink = successLinks.Last();
                await _blockchainService.SetBestChainAsync(chain, blockLink.Height, blockLink.BlockHash);
                await LocalEventBus.PublishAsync(new BestChainFoundEventData
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight,
                    ExecutedBlocks = successLinks.Select(p => p.BlockHash).ToList()
                });
            }

            Logger.LogInformation($"Attach blocks to best chain, status: {status}, best chain hash: {chain.BestChainHash}, height: {chain.BestChainHeight}");

            return blockLinks;
        }
    }
}