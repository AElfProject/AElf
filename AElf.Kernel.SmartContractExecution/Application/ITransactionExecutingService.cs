using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ITransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(IChainContext chainContext,
            List<Transaction> transactions, DateTime currentBlockTime, CancellationToken cancellationToken);
    }

    public class TransactionExecutingService : ITransactionExecutingService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly ITransactionResultManager _transactionResultManager;
        public ILogger<TransactionExecutingService> Logger { get; set; }

        public TransactionExecutingService(ITransactionResultManager transactionResultManager,
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _transactionResultManager = transactionResultManager;
            _smartContractExecutiveService = smartContractExecutiveService;
            Logger = NullLogger<TransactionExecutingService>.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(IChainContext chainContext,
            List<Transaction> transactions, DateTime currentBlockTime, CancellationToken cancellationToken)
        {
            var stateCache = new Dictionary<StatePath, StateCache>();
            var returnSets = new List<ExecutionReturnSet>();
            foreach (var transaction in transactions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var trace = await ExecuteOneAsync(0, chainContext, transaction, currentBlockTime,
                    cancellationToken, stateCache);
                if (!trace.IsSuccessful())
                {
                    trace.SurfaceUpError();
                }

                var result = GetTransactionResult(trace);
                if (result != null)
                {
                    await _transactionResultManager.AddTransactionResultAsync(result);
                    returnSets.Add(GetReturnSet(trace, result));
                }
            }

            return returnSets;
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, IChainContext chainContext,
            Transaction transaction, DateTime currentBlockTime, CancellationToken cancellationToken,
            Dictionary<StatePath, StateCache> stateCache)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace()
                {
                    TransactionId = transaction.GetHash(),
                    ExecutionStatus = ExecutionStatus.Canceled
                };
            }

            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = currentBlockTime,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight,
                Trace = trace,
                CallDepth = depth,
            };

            var executive =
                await _smartContractExecutiveService.GetExecutiveAsync(chainContext.ChainId, chainContext,
                    transaction.To);

            try
            {
                executive.SetDataCache(stateCache);
                await executive.SetTransactionContext(txCtxt).Apply();

                txCtxt.Trace.StateSet = new TransactionExecutingStateSet();
                foreach (var kv in txCtxt.Trace.StateChanges)
                {
                    // TODO: Better encapsulation/abstraction for committing to state cache
                    stateCache[kv.StatePath] = new StateCache(kv.StateValue.CurrentValue.ToByteArray());
                    var key = string.Join("/", kv.StatePath.Path.Select(x => x.ToStringUtf8()));
                    txCtxt.Trace.StateSet.Writes[key] = kv.StateValue.CurrentValue;
                }

                foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                {
                    var inlineTrace = await ExecuteOneAsync(depth + 1, chainContext, inlineTx,
                        currentBlockTime, cancellationToken, stateCache);
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
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext.ChainId, transaction.To, executive);
            }

            return trace;
        }

        private TransactionResult GetTransactionResult(TransactionTrace trace)
        {
            switch (trace.ExecutionStatus)
            {
                case ExecutionStatus.Canceled:
                    // Put back transaction
                    return null;
                case ExecutionStatus.ExecutedAndCommitted:
                    // Successful
                    var txRes = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId,
                        Status = TransactionResultStatus.Mined,
                        RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes()),
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
                case ExecutionStatus.ContractError:
                    var txResF = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId,
                        RetVal = ByteString.CopyFromUtf8(trace.StdErr), // Is this needed?
                        Status = TransactionResultStatus.Failed,
                        StateHash = Hash.Default
                    };
                    return txResF;
                case ExecutionStatus.InsufficientTransactionFees:
                    var txResITF = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId,
                        RetVal = ByteString.CopyFromUtf8(trace.ExecutionStatus.ToString()), // Is this needed?
                        Status = TransactionResultStatus.Failed,
                        StateHash = trace.GetSummarizedStateHash()
                    };
                    return txResITF;
                case ExecutionStatus.Undefined:
                    Logger.LogCritical(
                        $@"Transaction Id ""{
                                trace.TransactionId
                            } is executed with status Undefined. Transaction trace: {trace}""");
                    return null;
                case ExecutionStatus.SystemError:
                    // SystemError shouldn't happen, and need to fix
                    Logger.LogCritical(
                        $@"Transaction Id ""{
                                trace.TransactionId
                            } is executed with status SystemError. Transaction trace: {trace}""");
                    return null;
                case ExecutionStatus.ExecutedButNotCommitted:
                    // If this happens, there's problem with the code
                    Logger.LogCritical(
                        $@"Transaction Id ""{
                                trace.TransactionId
                            } is executed with status ExecutedButNotCommitted. Transaction trace: {
                                trace
                            }""");
                    return null;
                default:
                    return null;
            }
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

            foreach (var s in trace.StateSet.Writes)
            {
                returnSet.StateChanges.Add(s.Key, s.Value);
            }

            if (trace.RetVal == null)
            {
                throw new NullReferenceException("RetVal of trace is null.");
            }

            returnSet.ReturnValue = trace.RetVal.Data;

            return returnSet;
        }
    }
}