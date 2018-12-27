using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Common;
using AElf.Kernel.Managers;

namespace AElf.Execution.Execution
{
    public class SimpleExecutingService : IExecutingService
    {
        private ISmartContractService _smartContractService;
        private ITransactionTraceManager _transactionTraceManager;
        private IChainContextService _chainContextService;
        private IStateManager _stateManager;

        public SimpleExecutingService(ISmartContractService smartContractService,
            ITransactionTraceManager transactionTraceManager, IStateManager stateManager,
            IChainContextService chainContextService)
        {
            _smartContractService = smartContractService;
            _transactionTraceManager = transactionTraceManager;
            _chainContextService = chainContextService;
            _stateManager = stateManager;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken cancellationToken, DateTime currentBlockTime, Hash disambiguationHash = null,
            TransactionType transactionType = TransactionType.ContractTransaction)
        {
            var chainContext = await _chainContextService.GetChainContextAsync(chainId);
            var stateCache = new Dictionary<StatePath, StateCache>();
            var traces = new List<TransactionTrace>();
            foreach (var transaction in transactions)
            {
                var trace = await ExecuteOneAsync(0, transaction, chainId, chainContext, currentBlockTime, stateCache,
                    cancellationToken);
                //Console.WriteLine($"{transaction.GetHash().ToHex()} : {trace.ExecutionStatus.ToString()}");
                if (trace.IsSuccessful())
                {
                    if (trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
                    {
                        await trace.CommitChangesAsync(_stateManager);
                    }
                }
                else
                {
                    trace.SurfaceUpError();
                }

                if (_transactionTraceManager != null)
                {
                    // Will be null only in tests
                    await _transactionTraceManager.AddTransactionTraceAsync(trace, disambiguationHash);
                }

                traces.Add(trace);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

//            await _stateDictator.ApplyCachedDataAction(stateCache);
            return traces;
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, Transaction transaction, Hash chainId,
            IChainContext chainContext, DateTime currentBlockTime, Dictionary<StatePath, StateCache> stateCache,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace()
                {
                    TransactionId = transaction.GetHash(),
                    StdErr = "Execution Canceled",
                    ExecutionStatus = ExecutionStatus.Canceled
                };
            }

            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext()
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = currentBlockTime,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight,
                Trace = trace,
                CallDepth = depth
            };

            var executive = await _smartContractService.GetExecutiveAsync(transaction.To, chainId);
            try
            {
                executive.SetDataCache(stateCache);
                await executive.SetTransactionContext(txCtxt).Apply();

                foreach (var kv in txCtxt.Trace.StateChanges)
                {
                    // TODO: Better encapsulation/abstraction for committing to state cache
                    stateCache[kv.StatePath] = new StateCache(kv.StateValue.CurrentValue.ToByteArray());
                }

                foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                {
                    var inlineTrace = await ExecuteOneAsync(depth + 1, inlineTx, chainId, chainContext, 
                        currentBlockTime, stateCache, cancellationToken);
                    trace.InlineTraces.Add(inlineTrace);
                }
            }
            catch (Exception ex)
            {
                txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txCtxt.Trace.StdErr += ex + "\n";
            }
            finally
            {
                await _smartContractService.PutExecutiveAsync(transaction.To, executive);
            }

            return trace;
        }
    }
}