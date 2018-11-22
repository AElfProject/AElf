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
using AElf.Kernel.Storages;

namespace AElf.Execution.Execution
{
    public class SimpleExecutingService : IExecutingService
    {
        private ISmartContractService _smartContractService;
        private ITransactionTraceManager _transactionTraceManager;
        private IChainContextService _chainContextService;
        private IStateStore _stateStore;

        public SimpleExecutingService(ISmartContractService smartContractService,
            ITransactionTraceManager transactionTraceManager, IStateStore stateStore,
            IChainContextService chainContextService)
        {
            _smartContractService = smartContractService;
            _transactionTraceManager = transactionTraceManager;
            _chainContextService = chainContextService;
            _stateStore = stateStore;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken cancellationToken, Hash disambiguationHash=null)
        {
            var chainContext = await _chainContextService.GetChainContextAsync(chainId);
            var stateCache = new Dictionary<DataPath, StateCache>();
            var traces = new List<TransactionTrace>();
            transactions = SortTransactions(chainId, transactions);
            foreach (var transaction in transactions)
            {
                var trace = await ExecuteOneAsync(0, transaction, chainId, chainContext, stateCache, cancellationToken);
                //Console.WriteLine($"{transaction.GetHash().ToHex()} : {trace.ExecutionStatus.ToString()}");
                if (trace.IsSuccessful() && trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
                {
                    //Console.WriteLine($"tx executed successfully: {transaction.GetHash().ToHex()}");
                    await trace.CommitChangesAsync(_stateStore);
//                    await _stateDictator.ApplyCachedDataAction(bufferedStateUpdates);
//                    foreach (var kv in bufferedStateUpdates)
//                    {
//                        stateCache[kv.Key] = kv.Value;
//                    }
                }

                if (_transactionTraceManager != null)
                {
                    // Will be null only in tests
                    await _transactionTraceManager.AddTransactionTraceAsync(trace, disambiguationHash);    
                }

                traces.Add(trace);
            }

//            await _stateDictator.ApplyCachedDataAction(stateCache);
            return traces;
        }

        private List<Transaction> SortTransactions(Hash chainId, List<Transaction> transactions)
        {
            var contractZeroAddress = AddressHelpers.GetSystemContractAddress(chainId, SmartContractType.BasicContractZero.ToString());
            var result = new List<Transaction>();
            var contractTxs = new List<Transaction>();
            foreach (var tx in transactions)
            {
                if (tx.To.Equals(contractZeroAddress))
                {
                    contractTxs.Add(tx);
                }
                else
                {
                    result.Add(tx);
                }
            }
            result.AddRange(contractTxs);
            
            return result;
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