using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
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
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<FullBlockchainExecutingService> Logger { get; set; }
        
        public FullBlockchainExecutingService(IBlockchainService blockchainService,
            IBlockValidationService blockValidationService,
            IBlockExecutingService blockExecutingService,
            ITransactionResultService transactionResultService, IBlockStateSetManger blockStateSetManger)
        {
            _blockchainService = blockchainService;
            _blockValidationService = blockValidationService;
            _blockExecutingService = blockExecutingService;
            _transactionResultService = transactionResultService;
            _blockStateSetManger = blockStateSetManger;

            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<BlockExecutionResult> ExecuteBlocksAsync(IEnumerable<Block> blocks)
        {
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

                Logger.LogWarning(
                    $"Block validation failed: {ex.Message}. Inner exception {ex.InnerException.Message}");
            }

            return executionResult;
        }


        private async Task<BlockExecutedSet> ExecuteBlockAsync(Block block)
        {
            var blockHash = block.GetHash();

            var blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
            if (blockState != null)
            {
                Logger.LogInformation($"Block already executed. block hash: {blockHash}");
                return await GetExecuteBlockSetAsync(block, blockHash);
            }

            var transactions = await _blockchainService.GetTransactionsAsync(block.TransactionIds);
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            var executedBlock = blockExecutedSet.Block;

            var blockHashWithoutCache = executedBlock.GetHashWithoutCache();
            if (blockHashWithoutCache == blockHash) 
                return blockExecutedSet;
            Logger.LogWarning(
                $"Block execution failed. Expected: {block}, actual: {executedBlock}");
            return null;
        }

        private async Task<BlockExecutedSet> GetExecuteBlockSetAsync(Block block, Hash blockHash)
        {
            var set = new BlockExecutedSet()
            {
                Block = block,
                TransactionMap = new Dictionary<Hash,Transaction>(),
                    
                TransactionResultMap = new Dictionary<Hash, TransactionResult>()
            };
            if (block.TransactionIds.Any())
            {
                set.TransactionMap = (await _blockchainService.GetTransactionsAsync(block.TransactionIds))
                    .ToDictionary(p => p.GetHash(), p => p);
            }
            
            foreach (var transactionId in block.TransactionIds)
            {
                if ((set.TransactionResultMap[transactionId] =
                        await _transactionResultService.GetTransactionResultAsync(transactionId, blockHash))
                    == null)
                {
                    Logger.LogWarning(
                        $"fail to load transaction result. block hash : {blockHash}, tx id: {transactionId}");

                    return null;
                }
            }

            return set;
        }

        /// <summary>
        /// Processing pipeline for a block contains ValidateBlockBeforeExecute, ExecuteBlock and ValidateBlockAfterExecute.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>Block processing result is true if succeed, otherwise false.</returns>
        private async Task<BlockExecutedSet> ProcessBlockAsync(Block block)
        {
            var blockHash = block.GetHash();
            // Set the other blocks as bad block if found the first bad block
            if (!await _blockValidationService.ValidateBlockBeforeExecuteAsync(block))
            {
                Logger.LogWarning($"Block validate fails before execution. block hash : {blockHash}");
                return null;
            }

            var blockExecutedSet = await ExecuteBlockAsync(block);

            if (blockExecutedSet == null)
            {
                Logger.LogWarning($"Block execution failed. block hash : {blockHash}");
                return null;
            }

            if (!await _blockValidationService.ValidateBlockAfterExecuteAsync(block))
            {
                Logger.LogWarning($"Block validate fails after execution. block hash : {blockHash}");
                return null;
            }

            await _transactionResultService.ProcessTransactionResultAfterExecutionAsync(block.Header,
                block.Body.TransactionIds.ToList());

            return blockExecutedSet;
        }
    }
}