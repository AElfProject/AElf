using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Contexts;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ITransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(BlockHeader blockHeader, List<Transaction> transactions,
            CancellationToken cancellationToken);
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
            List<Transaction> transactions, CancellationToken cancellationToken)
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

            var trace = new TransactionTrace()
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
            };

            var internalStateCache = new TieredStateCache(chainContext.StateCache);
            var internalChainContext = new ChainContextWithTieredStateCache(chainContext, internalStateCache);
            var executive = await _smartContractExecutiveService.GetExecutiveAsync(
                internalChainContext,
                transaction.To);

            try
            {
                executive.SetDataCache(chainContext.StateCache);
                await executive.SetTransactionContext(txCtxt).Apply();

//                txCtxt.Trace.StateSet = new TransactionExecutingStateSet();
//                foreach (var kv in txCtxt.Trace.StateChanges)
//                {
//                    stateCache[kv.StatePath] = new StateCache(kv.StateValue.CurrentValue.ToByteArray());
//                    var key = string.Join("/", kv.StatePath.Path.Select(x => x.ToStringUtf8()));
//                    txCtxt.Trace.StateSet.Writes[key] = kv.StateValue.CurrentValue;
//                }

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
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(transaction.To, executive);
            }

            return trace;
        }

        private TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
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
                        ReturnValue = trace.RetVal.Data,
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
                case ExecutionStatus.ContractError:
                    var txResF = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId,
                        Status = TransactionResultStatus.Failed,
                        Error = trace.StdErr,
                        StateHash = Hash.Default
                    };
                    return txResF;
                case ExecutionStatus.InsufficientTransactionFees:
                    var txResITF = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId,
                        ReturnValue = ByteString.CopyFromUtf8(trace.ExecutionStatus.ToString()), // Is this needed?
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

            if (trace.IsSuccessful())
            {
                foreach (var s in trace.GetFlattenedWrite())
                {
                    returnSet.StateChanges[s.Key] = s.Value;
                }

                if (trace.RetVal == null)
                {
                    throw new NullReferenceException("RetVal of trace is null.");
                }

                returnSet.ReturnValue = trace.RetVal.Data;
            }

            return returnSet;
        }
    }
}