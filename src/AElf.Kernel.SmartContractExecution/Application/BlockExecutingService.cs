using System.Collections.Generic;
using System.Diagnostics;
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

            var returnSetContainer = new ReturnSetContainer(nonCancellableReturnSets);
            List<ExecutionReturnSet> cancellableReturnSets = new List<ExecutionReturnSet>();
            if (cancellable.Count > 0)
            {
                cancellableReturnSets =
                    await _executingService.ExecuteAsync(blockHeader, cancellable, cancellationToken, false,
                        returnSetContainer.ToBlockStateSet());
                returnSetContainer.AddRange(cancellableReturnSets);
            }

            Logger.LogTrace("Executed cancellable txs");

            Logger.LogTrace("Handled return set");

            if (returnSetContainer.Unexecutable.Count > 0)
            {
                await EventBus.PublishAsync(
                    new UnexecutableTransactionsFoundEvent(blockHeader, returnSetContainer.Unexecutable));
            }

            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executed.Contains(x.GetHash()))).ToList();
            stopwatch.Stop();
            Logger.LogWarning($"Filter timespan: {stopwatch.ElapsedMilliseconds} milliseconds");
            var block = await _blockGenerationService.FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions,
                returnSetContainer.Executed);

            Logger.LogTrace("Filled block");

            return block;
        }

        class ReturnSetContainer
        {
            private List<ExecutionReturnSet> _executed = new List<ExecutionReturnSet>();
            private List<Hash> _unexecutable = new List<Hash>();

            public List<ExecutionReturnSet> Executed => _executed;

            public List<Hash> Unexecutable => _unexecutable;

            public ReturnSetContainer(IEnumerable<ExecutionReturnSet> returnSets)
            {
                AddRange(returnSets);
            }

            public void AddRange(IEnumerable<ExecutionReturnSet> returnSets)
            {
                foreach (var returnSet in returnSets)
                {
                    if (returnSet.Status == TransactionResultStatus.Mined ||
                        returnSet.Status == TransactionResultStatus.Failed)
                    {
                        _executed.Add(returnSet);
                    }
                    else if (returnSet.Status == TransactionResultStatus.Unexecutable)
                    {
                        _unexecutable.Add(returnSet.TransactionId);
                    }
                }
            }

            public BlockStateSet ToBlockStateSet()
            {
                var blockStateSet = new BlockStateSet();
                foreach (var returnSet in _executed)
                {
                    foreach (var change in returnSet.StateChanges)
                    {
                        blockStateSet.Changes[change.Key] = change.Value;
                    }
                }

                return blockStateSet;
            }
        }
    }
}