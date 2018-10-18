using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public class SimpleExecutingService : IExecutingService
    {
        private ISmartContractService _smartContractService;
        private IStateDictator _stateDictator;
        private IChainContextService _chainContextService;

        public SimpleExecutingService(ISmartContractService smartContractService,
            IStateDictator stateDictator,
            IChainContextService chainContextService)
        {
            _smartContractService = smartContractService;
            _stateDictator = stateDictator;
            _chainContextService = chainContextService;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken cancellationToken)
        {
            var chainContext = await _chainContextService.GetChainContextAsync(chainId);
            var stateCache = new Dictionary<DataPath, StateCache>();
            var traces = new List<TransactionTrace>();
            foreach (var transaction in transactions)
            {
                var trace = await ExecuteOneAsync(0, transaction, chainId, chainContext, stateCache, cancellationToken);
                //Console.WriteLine($"{transaction.GetHash().ToHex()} : {trace.ExecutionStatus.ToString()}");
                if (trace.IsSuccessful() && trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
                {
                    //Console.WriteLine($"tx executed successfully: {transaction.GetHash().ToHex()}");
                    var bufferedStateUpdates = await trace.CommitChangesAsync(_stateDictator);
                    foreach (var kv in bufferedStateUpdates)
                    {
                        stateCache[kv.Key] = kv.Value;
                    }
                }

                traces.Add(trace);
            }

            await _stateDictator.ApplyCachedDataAction(stateCache);
            return traces;
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, Transaction transaction, Hash chainId,
            IChainContext chainContext, Dictionary<DataPath, StateCache> stateCache,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace()
                {
                    TransactionId = transaction.GetHash(),
                    StdErr = "Execution Canceled",
                    ExecutionStatus = ExecutionStatus.Canceled,
                    Transaction = transaction
                };
            }

            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash(),
                Transaction = transaction
            };

            var txCtxt = new TransactionContext()
            {
                PreviousBlockHash = chainContext.BlockHash,
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
                foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                {
                    var inlineTrace = await ExecuteOneAsync(depth + 1, inlineTx, chainId, chainContext, stateCache,
                        cancellationToken);
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