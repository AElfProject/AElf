using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingService : IBlockExecutingService, ITransientDependency
    {
        private readonly ITransactionExecutingService _executingService;
        private readonly IBlockManager _blockManager;
        private readonly IBlockGenerationService _blockGenerationService;
        public ILocalEventBus EventBus { get; set; }
        public ILogger<BlockExecutingService> Logger { get; set; }

        public BlockExecutingService(ITransactionExecutingService executingService, IBlockManager blockManager,
            IBlockGenerationService blockGenerationService)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockGenerationService = blockGenerationService;
            EventBus = NullLocalEventBus.Instance;
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
            Logger.LogTrace("Entered ExecuteBlockAsync");
            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();

            var nonCancellableReturnSets =
                await _executingService.ExecuteAsync(blockHeader, nonCancellable, CancellationToken.None, true);
            Logger.LogTrace("Executed non-cancellable txs");

            var cancellableReturnSets =
                await _executingService.ExecuteAsync(blockHeader, cancellable, cancellationToken, false);
            Logger.LogTrace("Executed cancellable txs");

            var blockReturnSet = new List<ExecutionReturnSet>();
            var unexecutable = new List<Hash>();
            foreach (var returnSet in nonCancellableReturnSets.Concat(cancellableReturnSets))
            {
                if (returnSet.Status == TransactionResultStatus.Mined ||
                    returnSet.Status == TransactionResultStatus.Failed)
                {
                    blockReturnSet.Add(returnSet);
                }else if (returnSet.Status == TransactionResultStatus.Unexecutable)
                {
                    unexecutable.Add(returnSet.TransactionId);
                }
            }
            Logger.LogTrace("Handled return set");

            if (unexecutable.Count > 0)
            {
                await EventBus.PublishAsync(new UnexecutableTransactionsFoundEvent(blockHeader, unexecutable));
            }
            
            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executed.Contains(x.GetHash()))).ToList();
            var block = await _blockGenerationService.FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions,
                blockReturnSet);

            Logger.LogTrace("Filled block");

            return block;
        }
    }
}