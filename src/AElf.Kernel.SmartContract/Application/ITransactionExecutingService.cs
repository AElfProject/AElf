using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(BlockHeader blockHeader, List<Transaction> transactions,
            CancellationToken cancellationToken, bool throwException = false);
    }

    public class TransactionExecutingService : ITransactionExecutingService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly ITransactionResultService _transactionResultService;
        public ILogger<TransactionExecutingService> Logger { get; set; }

        public TransactionExecutingService(ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _transactionResultService = transactionResultService;
            _smartContractExecutiveService = smartContractExecutiveService;
            Logger = NullLogger<TransactionExecutingService>.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(BlockHeader blockHeader,
            List<Transaction> transactions, CancellationToken cancellationToken, bool throwException)
        {
            var groupStateCache = new TieredStateCache();
            var groupChainContext = new ChainContextWithTieredStateCache(blockHeader.PreviousBlockHash,
                blockHeader.Height - 1, groupStateCache);

            var returnSets = new List<ExecutionReturnSet>();
            foreach (var transaction in transactions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var trace = await ExecuteOneAsync(0, groupChainContext, transaction, blockHeader.Time.ToDateTime(),
                    cancellationToken);
                if (!trace.IsSuccessful())
                {
                    if (throwException)
                    {
                        Logger.LogError(trace.StdErr);
                    }
                    trace.SurfaceUpError();
                }
                else
                {
                    groupStateCache.Update(trace.GetFlattenedWrite()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                }

                if (trace.StdErr != string.Empty)
                {
                    Logger.LogError(trace.StdErr);
                }

                var result = GetTransactionResult(trace, blockHeader.Height);

                if (result != null)
                {
                    await _transactionResultService.AddTransactionResultAsync(result, blockHeader);
                }

                returnSets.Add(GetReturnSet(trace, result));
            }

            return returnSets;
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, IChainContext chainContext,
            Transaction transaction, DateTime currentBlockTime, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace()
                {
                    TransactionId = transaction.GetHash(),
                    ExecutionStatus = ExecutionStatus.Canceled
                };
            }

            if (transaction.To == null || transaction.From == null)
            {
                throw new Exception($"error tx: {transaction}");
            }
            var trace = new TransactionTrace
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = currentBlockTime,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = depth,
                StateCache = chainContext.StateCache
            };

            var internalStateCache = new TieredStateCache(chainContext.StateCache);
            var internalChainContext = new ChainContextWithTieredStateCache(chainContext, internalStateCache);
            var executive = await _smartContractExecutiveService.GetExecutiveAsync(
                internalChainContext,
                transaction.To);

            try
            {
                await executive.ApplyAsync(txCtxt);

                if (txCtxt.Trace.IsSuccessful() && txCtxt.Trace.InlineTransactions.Count > 0)
                {
                    internalStateCache.Update(txCtxt.Trace.GetFlattenedWrite()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                    foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                    {
                        var inlineTrace = await ExecuteOneAsync(depth + 1, internalChainContext, inlineTx,
                            currentBlockTime, cancellationToken);
                        trace.InlineTraces.Add(inlineTrace);
                        if (!inlineTrace.IsSuccessful())
                        {
                            // Fail already, no need to execute remaining inline transactions
                            break;
                        }

                        internalStateCache.Update(inlineTrace.GetFlattenedWrite()
                            .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                    }
                }
            }
            catch (Exception ex)
            {
                txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txCtxt.Trace.StdErr += ex + "\n";
                throw;
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(transaction.To, executive);
            }

            return trace;
        }

        private TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            if (trace.ExecutionStatus == ExecutionStatus.Undefined)
            {
                return null;
            }

            if (trace.IsSuccessful())
            {
                var txRes = new TransactionResult()
                {
                    TransactionId = trace.TransactionId,
                    Status = TransactionResultStatus.Mined,
                    ReturnValue = trace.ReturnValue,
                    ReadableReturnValue = trace.ReadableReturnValue,
                    BlockNumber = blockHeight,
                    //StateHash = trace.GetSummarizedStateHash(),
                    Logs = {trace.FlattenedLogs}
                };
                txRes.UpdateBloom();

                // insert deferred txn to transaction pool and wait for execution 
                if (trace.DeferredTransaction.Length != 0)
                {
                    var deferredTxn = Transaction.Parser.ParseFrom(trace.DeferredTransaction);
                    txRes.DeferredTransactions.Add(deferredTxn);
                    txRes.DeferredTxnId = deferredTxn.GetHash();
                }

                return txRes;
            }

            return new TransactionResult()
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Failed,
                Error = trace.StdErr
            };
        }

        private ExecutionReturnSet GetReturnSet(TransactionTrace trace, TransactionResult result)
        {
            var returnSet = new ExecutionReturnSet()
            {
                TransactionId = result.TransactionId,
                Status = result.Status,
                Bloom = result.Bloom
            };

            foreach (var tx in result.DeferredTransactions)
            {
                returnSet.DeferredTransactions.Add(tx);
            }

            if (trace.IsSuccessful())
            {
                foreach (var s in trace.GetFlattenedWrite())
                {
                    returnSet.StateChanges[s.Key] = s.Value;
                }

                returnSet.ReturnValue = trace.ReturnValue;
            }

            return returnSet;
        }
    }
}