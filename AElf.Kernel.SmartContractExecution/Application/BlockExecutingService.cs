using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingService : IBlockExecutingService, ITransientDependency
    {
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IBlockManager _blockManager;
        private readonly ITransactionExecutingService _executingService;

        public BlockExecutingService(ITransactionExecutingService executingService, IBlockManager blockManager,
            IBlockGenerationService blockGenerationService)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockGenerationService = blockGenerationService;
        }

        public async Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            return await ExecuteBlockAsync(blockHeader, nonCancellableTransactions, new List<Transaction>(),
                CancellationToken.None);
        }

        public async Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions, IEnumerable<Transaction> cancellableTransactions,
            CancellationToken cancellationToken)
        {
            // TODO: If already executed, don't execute again. Maybe check blockStateSet?

            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();

            var chainContext = new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash,
                BlockHeight = blockHeader.Height - 1
            };
            var nonCancellableReturnSets =
                await _executingService.ExecuteAsync(blockHeader, nonCancellable, CancellationToken.None, true);
            var cancellableReturnSets =
                await _executingService.ExecuteAsync(blockHeader, cancellable, cancellationToken, false);
            var blockReturnSet = nonCancellableReturnSets.Concat(cancellableReturnSets);

            // TODO: Insert deferredTransactions to TxPool

            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executed.Contains(x.GetHash()))).ToList();
            var block = await _blockGenerationService.FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions,
                blockReturnSet);

            return block;
        }
    }
}