using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution.Events;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    public class TransactionExecutingService : ITransactionExecutingService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly List<IPreExecutionPlugin> _prePlugins;
        private readonly List<IPostExecutionPlugin> _postPlugins;
        private readonly ITransactionResultService _transactionResultService;
        public ILogger Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public TransactionExecutingService(ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService, IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins
            )
        {
            _transactionResultService = transactionResultService;
            _smartContractExecutiveService = smartContractExecutiveService;
            _prePlugins = GetUniquePrePlugins(prePlugins);
            _postPlugins = GetUniquePostPlugins(postPlugins);
            Logger = NullLogger<TransactionExecutingService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException)
        {
            var groupStateCache = transactionExecutingDto.PartialBlockStateSet == null
                ? new TieredStateCache()
                : new TieredStateCache(
                    new StateCacheFromPartialBlockStateSet(transactionExecutingDto.PartialBlockStateSet));
            var groupChainContext = new ChainContextWithTieredStateCache(
                transactionExecutingDto.BlockHeader.PreviousBlockHash,
                transactionExecutingDto.BlockHeader.Height - 1, groupStateCache);

            var returnSets = new List<ExecutionReturnSet>();
            Logger.LogTrace($"###=======### start enumerating transactions and transaction count is {transactionExecutingDto.Transactions.Count()}");
            foreach (var transaction in transactionExecutingDto.Transactions)
            {
                TransactionTrace trace;
//                if (cancellationToken.IsCancellationRequested)
//                {
//                    break;
//                }
//                trace = await ExecuteOneAsync(0, groupChainContext, transaction, 
//                    transactionExecutingDto.BlockHeader.Time,
//                    cancellationToken);
                try
                {
                    trace = await ExecuteOneAsync(0, groupChainContext, transaction, 
                        transactionExecutingDto.BlockHeader.Time,
                        cancellationToken).WithCancellation(cancellationToken);
                }
                catch(Exception e)
                {
                    Logger.LogTrace("###=======###canceled in executeAsync  in foreach transaction");
                    break;
                }
                if (trace == null)
                    break;
                // Will be useful when debugging MerkleTreeRootOfWorldState is different from each miner.
                Logger.LogTrace(transaction.MethodName);
                Logger.LogTrace(trace.StateSet.Writes.Values.Select(v => v.ToBase64().ComputeHash().ToHex())
                    .JoinAsString("\n"));

                if (!trace.IsSuccessful())
                {
                    if (throwException)
                    {
                        Logger.LogError(trace.Error);
                    }

                    // Do not package this transaction if any of his inline transactions canceled.
                    if (IsTransactionCanceled(trace))
                    {
                        break;
                    }

                    trace.SurfaceUpError();
                }
                else
                {
                    groupStateCache.Update(trace.GetFlattenedWrites()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                }

                if (trace.Error != string.Empty)
                {
                    Logger.LogError(trace.Error);
                }

                var result = GetTransactionResult(trace, transactionExecutingDto.BlockHeader.Height);

                if (result != null)
                {
                    await _transactionResultService.AddTransactionResultAsync(result,
                        transactionExecutingDto.BlockHeader);
                }

                var returnSet = GetReturnSet(trace, result);
                returnSets.Add(returnSet);
            }

            return returnSets;
        }

        private bool IsTransactionCanceled(TransactionTrace trace)
        {
            return trace.ExecutionStatus == ExecutionStatus.Canceled ||
                   trace.InlineTraces.ToList().Any(IsTransactionCanceled);
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, IChainContext chainContext,
            Transaction transaction, Timestamp currentBlockTime, CancellationToken cancellationToken,
            Address origin = null, bool isCancellable = true)
        {
            await Task.Yield();
//            if (cancellationToken.IsCancellationRequested && isCancellable)
//            {
//                return new TransactionTrace
//                {
//                    TransactionId = transaction.GetHash(),
//                    ExecutionStatus = ExecutionStatus.Canceled,
//                    Error = "Execution cancelled"
//                };
//            }
            Logger.LogTrace("###====### smart contract prepared");
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
                StateCache = chainContext.StateCache,
                Origin = origin != null ? origin : transaction.From
            };
            var internalStateCache = new TieredStateCache(chainContext.StateCache);
            var internalChainContext = new ChainContextWithTieredStateCache(chainContext, internalStateCache);

            IExecutive executive;
//            try
//            {
//                executive = await _smartContractExecutiveService.GetExecutiveAsync(
//                    internalChainContext,
//                    transaction.To).WithCancellation(cancellationToken);
//            }
//            catch (SmartContractFindRegistrationException e)
//            {
//                txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
//                txCtxt.Trace.Error += "Invalid contract address.\n";
//                return trace;
//            }
            try
            {
                Logger.LogTrace("######=======####### prepare to execute smart contract");//get, cancel directly
                executive = await _smartContractExecutiveService.GetExecutiveAsync(
                    internalChainContext,
                    transaction.To);//.WithCancellation(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogTrace("######=======####### cancel in 158");
                trace.ExecutionStatus = ExecutionStatus.Canceled;
                txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txCtxt.Trace.Error += "Invalid contract address.\n";
                return trace;
            }
            
            try
            {
                #region PreTransaction

                if (depth == 0)
                {
                    if (!await ExecutePluginOnPreTransactionStageAsync(executive, txCtxt, currentBlockTime,
                        internalChainContext, internalStateCache, cancellationToken))
                    {
                        return trace;
                    }
                }

                #endregion
                //await executive.ApplyAsync(txCtxt);
                try
                {
                    await executive.ApplyAsync(txCtxt); //.WithCancellation(cancellationToken);
                }
                catch
                {
                    Logger.LogTrace("timeout in execute one async in executive.ApplyAsync(txCtxt)");
                    trace.ExecutionStatus = ExecutionStatus.Canceled;
                    return trace;
                }
//                await ExecuteInlineTransactions(depth, currentBlockTime, txCtxt, internalStateCache,
//                    internalChainContext, cancellationToken);
                try
                {
                    await ExecuteInlineTransactions(depth, currentBlockTime, txCtxt, internalStateCache,
                        internalChainContext, cancellationToken); //.WithCancellation(cancellationToken);
                }
                catch
                {
                    Logger.LogTrace("timeout in execute one async in ExecuteInlineTransactions");
                    trace.ExecutionStatus = ExecutionStatus.Canceled;
                    return trace;
                }

                #region PostTransaction

                if (depth == 0)
                {
//                    bool isSuccess = await ExecutePluginOnPostTransactionStageAsync(executive, txCtxt,
//                        currentBlockTime,
//                        internalChainContext, internalStateCache, cancellationToken);
                    bool isSuccess;
                    try
                    {
                        isSuccess = await ExecutePluginOnPostTransactionStageAsync(executive, txCtxt,
                            currentBlockTime,
                            internalChainContext, internalStateCache,
                            cancellationToken); //.WithCancellation(cancellationToken);
                    }
                    catch
                    {
                        Logger.LogTrace("######=======####### canceled in plugin post transaction");
                        trace.ExecutionStatus = ExecutionStatus.Canceled;
                        return trace;
                    }
                    if (!isSuccess)
                    {
                        return trace;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txCtxt.Trace.Error += ex + "\n";
                throw;
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(transaction.To, executive);
                await LocalEventBus.PublishAsync(new TransactionExecutedEventData
                {
                    TransactionTrace = trace
                });
            }
            return trace;
        }

        private async Task ExecuteInlineTransactions(int depth, Timestamp currentBlockTime,
            TransactionContext txCtxt, TieredStateCache internalStateCache,
            ChainContextWithTieredStateCache internalChainContext, CancellationToken cancellationToken)
        {
            var trace = txCtxt.Trace;
            if (txCtxt.Trace.IsSuccessful() && txCtxt.Trace.InlineTransactions.Count > 0)
            {
                internalStateCache.Update(txCtxt.Trace.GetFlattenedWrites()
                    .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                {
                    TransactionTrace inlineTrace = null;
                    inlineTrace = await ExecuteOneAsync(depth + 1, internalChainContext, inlineTx,
                        currentBlockTime, cancellationToken, txCtxt.Origin).WithCancellation(cancellationToken);
//                    try
//                    {
//                        inlineTrace = await ExecuteOneAsync(depth + 1, internalChainContext, inlineTx,
//                            currentBlockTime, cancellationToken, txCtxt.Origin).WithCancellation(cancellationToken);
//                    }
//                    catch
//                    {
//                        Logger.LogDebug("canceled in inline transaction");
//                        break;
//                    }
                    trace.InlineTraces.Add(inlineTrace);
                    if (!inlineTrace.IsSuccessful())
                    {
                        Logger.LogError($"Method name: {inlineTx.MethodName}, {inlineTrace.Error}");
                        // Fail already, no need to execute remaining inline transactions
                        break;
                    }

                    internalStateCache.Update(inlineTrace.GetFlattenedWrites()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                }
            }
        }

        private async Task<bool> ExecutePluginOnPreTransactionStageAsync(IExecutive executive,
            ITransactionContext txCtxt,
            Timestamp currentBlockTime, ChainContextWithTieredStateCache internalChainContext,
            TieredStateCache internalStateCache,
            CancellationToken cancellationToken)
        {
            var trace = txCtxt.Trace;
            foreach (var plugin in _prePlugins)
            {
                IEnumerable<Transaction> transactions = null;
                transactions = await plugin.GetPreTransactionsAsync(executive.Descriptors, txCtxt);
//                try
//                {
//                    transactions = await plugin.GetPreTransactionsAsync(executive.Descriptors, txCtxt)
//                        .WithCancellation(cancellationToken);
//                }
//                catch
//                {
//                    Logger.LogTrace("timeout in async plugin");
//                }
                foreach (var preTx in transactions)
                {
                    TransactionTrace preTrace;
                    preTrace = await ExecuteOneAsync(0, internalChainContext, preTx, currentBlockTime,
                        cancellationToken);
//                    try
//                    {
//                        preTrace = await ExecuteOneAsync(0, internalChainContext, preTx, currentBlockTime,
//                            cancellationToken).WithCancellation(cancellationToken);
//                    }
//                    catch
//                    {
//                        Logger.LogDebug("canceled in ExecutePluginOnPreTransactionStageAsync");
//                        return false;
//                    }
                    trace.PreTransactions.Add(preTx);
                    trace.PreTraces.Add(preTrace);
                    if (!preTrace.IsSuccessful())
                    {
                        trace.ExecutionStatus = ExecutionStatus.Prefailed;
                        preTrace.SurfaceUpError();
                        trace.Error += preTrace.Error;
                        return false;
                    }

                    internalStateCache.Update(preTrace.GetFlattenedWrites()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                }
            }

            return true;
        }

        private async Task<bool> ExecutePluginOnPostTransactionStageAsync(IExecutive executive,
            ITransactionContext txCtxt,
            Timestamp currentBlockTime, ChainContextWithTieredStateCache internalChainContext,
            TieredStateCache internalStateCache,
            CancellationToken cancellationToken)
        {
            var trace = txCtxt.Trace;
            foreach (var plugin in _postPlugins)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                var transactions = await plugin.GetPostTransactionsAsync(executive.Descriptors, txCtxt);
                foreach (var postTx in transactions)
                {
                    TransactionTrace postTrace;
                    postTrace = await ExecuteOneAsync(0, internalChainContext, postTx, currentBlockTime,
                        cancellationToken).WithCancellation(cancellationToken);
//                    try
//                    {
//                        postTrace = await ExecuteOneAsync(0, internalChainContext, postTx, currentBlockTime,
//                            cancellationToken).WithCancellation(cancellationToken);
//                    }
//                    catch
//                    {
//                        return false;
//                    }
                    trace.PostTransactions.Add(postTx);
                    trace.PostTraces.Add(postTrace);
                    if (!postTrace.IsSuccessful())
                    {
                        trace.ExecutionStatus = ExecutionStatus.Postfailed;
                        postTrace.SurfaceUpError();
                        trace.Error += postTrace.Error;
                        return false;
                    }

                    internalStateCache.Update(postTrace.GetFlattenedWrites()
                        .Select(x => new KeyValuePair<string, byte[]>(x.Key, x.Value.ToByteArray())));
                }
            }

            return true;
        }

        private TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            if (trace.ExecutionStatus == ExecutionStatus.Undefined)
            {
                return null;
            }

            if (trace.ExecutionStatus == ExecutionStatus.Prefailed)
            {
                return new TransactionResult
                {
                    TransactionId = trace.TransactionId,
                    Status = TransactionResultStatus.Unexecutable,
                    Error = trace.Error
                };
            }

            if (trace.IsSuccessful())
            {
                var txRes = new TransactionResult
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

                return txRes;
            }

            return new TransactionResult
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Failed,
                Error = trace.Error
            };
        }

        private ExecutionReturnSet GetReturnSet(TransactionTrace trace, TransactionResult result)
        {
            var returnSet = new ExecutionReturnSet
            {
                TransactionId = result.TransactionId,
                Status = result.Status,
                Bloom = result.Bloom
            };

            if (trace.IsSuccessful())
            {
                foreach (var s in trace.GetFlattenedWrites())
                {
                    returnSet.StateChanges[s.Key] = s.Value;
                }

                returnSet.ReturnValue = trace.ReturnValue;
            }

            foreach (var s in trace.GetFlattenedReads())
            {
                returnSet.StateAccesses[s.Key] = s.Value;
            }

            return returnSet;
        }

        private static List<IPreExecutionPlugin> GetUniquePrePlugins(IEnumerable<IPreExecutionPlugin> plugins)
        {
            // One instance per type
            return plugins.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }

        private static List<IPostExecutionPlugin> GetUniquePostPlugins(IEnumerable<IPostExecutionPlugin> plugins)
        {
            // One instance per type
            return plugins.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }
    }
    public static class ObjExt{
        private struct Void { }
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> originalTask, CancellationToken ct) 
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(new Void()), cancelTask)) 
            {
                var any = await Task.WhenAny(originalTask, cancelTask.Task);
                if (any == cancelTask.Task) {
                    ct.ThrowIfCancellationRequested();                 
                }
            }
            return await originalTask;
        }
        public static async Task WithCancellation(this Task originalTask, CancellationToken ct)
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(new Void()), cancelTask))
            {
                Task any = await Task.WhenAny(originalTask, cancelTask.Task);
                if (any == cancelTask.Task)
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            await originalTask;
        }
    }
}