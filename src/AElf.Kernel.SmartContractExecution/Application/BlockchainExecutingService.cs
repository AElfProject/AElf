using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class FullBlockchainExecutingService : IBlockchainExecutingService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly ITransactionResultService _transactionResultService;
        private readonly IExecutedTransactionResultCacheProvider _executedTransactionResultCacheProvider;
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<FullBlockchainExecutingService> Logger { get; set; }

        public FullBlockchainExecutingService(IBlockchainService blockchainService,
            IBlockValidationService blockValidationService,
            IBlockExecutingService blockExecutingService,
            ITransactionResultService transactionResultService, IBlockStateSetManger blockStateSetManger, IExecutedTransactionResultCacheProvider executedTransactionResultCacheProvider)
        {
            _blockchainService = blockchainService;
            _blockValidationService = blockValidationService;
            _blockExecutingService = blockExecutingService;
            _transactionResultService = transactionResultService;
            _blockStateSetManger = blockStateSetManger;
            _executedTransactionResultCacheProvider = executedTransactionResultCacheProvider;

            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<BlockExecutionResult> ExecuteBlocksAsync(IEnumerable<Block> blocks)
        {
            Logger.LogTrace("Begin FullBlockchainExecutingService.ExecuteBlocksAsync");
            var executionResult = new BlockExecutionResult();
            try
            {
                foreach (var block in blocks)
                {
                    var blockExecutedSet = await ProcessBlockAsync(block);
                    if (blockExecutedSet == null)
                    {
                        executionResult.ExecutedFailedBlocks.Add(block);
                        return executionResult;
                    }

                    executionResult.SuccessBlockExecutedSets.Add(blockExecutedSet);
                    Logger.LogInformation(
                        $"Executed block {block.GetHash()} at height {block.Height}, with {block.Body.TransactionsCount} txns.");

                    await LocalEventBus.PublishAsync(new BlockAcceptedEvent {BlockExecutedSet = blockExecutedSet});
                }
            }
            catch (BlockValidationException ex)
            {
                if (!(ex.InnerException is ValidateNextTimeBlockValidationException))
                {
                    throw;
                }

                Logger.LogDebug(
                    $"Block validation failed: {ex.Message}. Inner exception {ex.InnerException.Message}");
            }
            Logger.LogTrace("End FullBlockchainExecutingService.ExecuteBlocksAsync");
            return executionResult;
        }


        private async Task<BlockExecutedSet> ExecuteBlockAsync(Block block)
        {
            Logger.LogTrace("Begin FullBlockchainExecutingService.ExecuteBlockAsync");
            var blockHash = block.GetHash();

            var blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
            if (blockState != null)
            {
                Logger.LogDebug($"Block already executed. block hash: {blockHash}");
                return await GetExecuteBlockSetAsync(block, blockHash);
            }

            var transactions = await _blockchainService.GetTransactionsAsync(block.TransactionIds);
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            var executedBlock = blockExecutedSet.Block;

            var blockHashWithoutCache = executedBlock.GetHashWithoutCache();
            
            Logger.LogTrace("End FullBlockchainExecutingService.ExecuteBlockAsync");
            
            if (blockHashWithoutCache == blockHash)
                return blockExecutedSet;
            Logger.LogDebug(
                $"Block execution failed. Expected: {block}, actual: {executedBlock}");
            return null;
        }

        private async Task<BlockExecutedSet> GetExecuteBlockSetAsync(Block block, Hash blockHash)
        {
            var set = new BlockExecutedSet()
            {
                Block = block,
                TransactionResults = new List<TransactionResult>()
            };

            Logger.LogDebug("GetExecuteBlockSetAsync - 1");

            var transactionResult = _executedTransactionResultCacheProvider.GetTransactionResults(block.GetHash());
            if (transactionResult != null)
            {
                set.TransactionResults = transactionResult;
            }
            else
            {
                set.TransactionResults = await _transactionResultService.GetTransactionResultsAsync(block.Body.TransactionIds, blockHash);
            }

            Logger.LogDebug("GetExecuteBlockSetAsync - 3");


            return set;
        }

        /// <summary>
        /// Processing pipeline for a block contains ValidateBlockBeforeExecute, ExecuteBlock and ValidateBlockAfterExecute.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>Block processing result is true if succeed, otherwise false.</returns>
        private async Task<BlockExecutedSet> ProcessBlockAsync(Block block)
        {
            Logger.LogTrace("Begin FullBlockchainExecutingService.ProcessBlockAsync");
            var blockHash = block.GetHash();

            var blockExecutedSet = await ExecuteBlockAsync(block);

            if (blockExecutedSet == null)
            {
                Logger.LogDebug($"Block execution failed. block hash : {blockHash}");
                return null;
            }

            Logger.LogDebug($"ProcessBlockAsync - 1");
            
            Logger.LogTrace("End FullBlockchainExecutingService.ProcessBlockAsync");
            return blockExecutedSet;
        }
    }
}