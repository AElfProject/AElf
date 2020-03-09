using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
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
                    var processResult = await TryProcessBlockAsync(block);
                    if (!processResult)
                    {
                        executionResult.ExecutedFailedBlocks.Add(block);
                        return executionResult;
                    }

                    executionResult.ExecutedSuccessBlocks.Add(block);
                    Logger.LogInformation(
                        $"Executed block {block.GetHash()} at height {block.Height}, with {block.Body.TransactionsCount} txns.");

                    await LocalEventBus.PublishAsync(new BlockAcceptedEvent {Block = block});
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

        private async Task<bool> TryExecuteBlockAsync(Block block)
        {
            var blockHash = block.GetHash();

            var blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
            if (blockState != null)
                return true;

            var transactions = await _blockchainService.GetTransactionsAsync(block.TransactionIds);
            var executedBlock = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);

            var blockHashWithoutCache = executedBlock.GetHashWithoutCache();

            if (blockHashWithoutCache != blockHash)
            {
                blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHashWithoutCache);
                Logger.LogWarning($"Block execution failed. BlockStateSet: {blockState}");
                Logger.LogWarning(
                    $"Block execution failed. Block header: {executedBlock.Header}, Block body: {executedBlock.Body}");

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

            await _transactionResultService.ProcessTransactionResultAfterExecutionAsync(block.Header,
                block.Body.TransactionIds.ToList());

            return true;
        }
    }
}