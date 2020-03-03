using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution.Events;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class TransactionExecutor : ITransactionExecutor, ISingletonDependency
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly IInlineTransactionValidationService _inlineTransactionValidationService;
        private readonly List<IPreExecutionPlugin> _prePlugins;
        private readonly List<IPostExecutionPlugin> _postPlugins;
        private readonly ITransactionResultService _transactionResultService;
        public ILogger<TransactionExecutor> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public TransactionExecutor(ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService,
            IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
            IInlineTransactionValidationService inlineTransactionValidationService)
        {
            _transactionResultService = transactionResultService;
            _smartContractExecutiveService = smartContractExecutiveService;
            _inlineTransactionValidationService = inlineTransactionValidationService;
            _prePlugins = GetUniquePrePlugins(prePlugins);
            _postPlugins = GetUniquePostPlugins(postPlugins);
            Logger = NullLogger<TransactionExecutor>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException)
        {
            try
            {
                var groupStateCache = transactionExecutingDto.PartialBlockStateSet == null
                    ? new TieredStateCache()
                    : new TieredStateCache(
                        new StateCacheFromPartialBlockStateSet(transactionExecutingDto.PartialBlockStateSet));
                var groupChainContext = new ChainContextWithTieredStateCache(
                    transactionExecutingDto.BlockHeader.PreviousBlockHash,
                    transactionExecutingDto.BlockHeader.Height - 1, groupStateCache);

                var transactionResults = new List<TransactionResult>();
                var returnSets = new List<ExecutionReturnSet>();
                foreach (var transaction in transactionExecutingDto.Transactions)
                {
                    TransactionTrace trace;
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var singleTxExecutingDto = new SingleTransactionExecutingDto
                    {
                        Depth = 0,
                        ChainContext = groupChainContext,
                        Transaction = transaction,
                        CurrentBlockTime = transactionExecutingDto.BlockHeader.Time,
                    };
                    try
                    {
                        var transactionExecutionTask = Task.Run(() => ExecuteOneAsync(singleTxExecutingDto,
                            cancellationToken), cancellationToken);

                        trace = await transactionExecutionTask.WithCancellation(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogTrace("Transaction canceled.");
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        continue;
                    }

                    if (trace == null)
                        break;

                    if (!trace.IsSuccessful())
                    {
#if DEBUG
                        if (throwException)	
                        {
                            Logger.LogError(trace.Error);	
                        }
#endif
                        // Do not package this transaction if any of his inline transactions canceled.
                        if (IsTransactionCanceled(trace))
                        {
                            break;
                        }

                        var transactionExecutingStateSets = new List<TransactionExecutingStateSet>();
                        foreach (var preTrace in trace.PreTraces)
                        {
                            if (preTrace.IsSuccessful())
                                transactionExecutingStateSets.AddRange(preTrace.GetStateSets());
                        }

                        foreach (var postTrace in trace.PostTraces)
                        {
                            if (postTrace.IsSuccessful())
                                transactionExecutingStateSets.AddRange(postTrace.GetStateSets());
                        }

                        groupStateCache.Update(transactionExecutingStateSets);
                        trace.SurfaceUpError();
                    }
                    else
                    {
                        groupStateCache.Update(trace.GetStateSets());
                    }
#if DEBUG
                    if (trace.Error != string.Empty)	
                    {	
                        Logger.LogError(trace.Error);	
                    }         
#endif
                    var result = GetTransactionResult(trace, transactionExecutingDto.BlockHeader.Height);

                    result.TransactionFee = trace.TransactionFee;
                    result.ConsumedResourceTokens = trace.ConsumedResourceTokens;
                    transactionResults.Add(result);

                    var returnSet = GetReturnSet(trace, result);
                    returnSets.Add(returnSet);
                }

                await _transactionResultService.AddTransactionResultsAsync(transactionResults,
                    transactionExecutingDto.BlockHeader);
                return returnSets;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed while executing txs in block.");
                throw;
            }
        }

        private static bool IsTransactionCanceled(TransactionTrace trace)
        {
            return trace.ExecutionStatus == ExecutionStatus.Canceled || trace.PreTraces.Any(IsTransactionCanceled) ||
                   trace.InlineTraces.Any(IsTransactionCanceled) || trace.PostTraces.Any(IsTransactionCanceled);
        }

        private async Task<TransactionTrace> ExecuteOneAsync(SingleTransactionExecutingDto singleTxExecutingDto, 
            CancellationToken cancellationToken)
        {
            if (singleTxExecutingDto.IsCancellable)
                cancellationToken.ThrowIfCancellationRequested();

            var txContext = CreateTransactionContext(singleTxExecutingDto, out var trace);

            var internalStateCache = new TieredStateCache(singleTxExecutingDto.ChainContext.StateCache);
            var internalChainContext = new ChainContextWithTieredStateCache(singleTxExecutingDto.ChainContext, internalStateCache);

            IExecutive executive;
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(
                    internalChainContext,
                    singleTxExecutingDto.Transaction.To);
            }
            catch (SmartContractFindRegistrationException)
            {
                txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txContext.Trace.Error += "Invalid contract address.\n";
                return trace;
            }

            try
            {
                #region PreTransaction

                if (singleTxExecutingDto.Depth == 0)
                {
                    if (!await ExecutePluginOnPreTransactionStageAsync(executive, txContext, singleTxExecutingDto.CurrentBlockTime,
                        internalChainContext, internalStateCache, cancellationToken))
                    {
                        trace.ExecutionStatus = ExecutionStatus.Prefailed;
                        return trace;
                    }
                }

                #endregion

                await executive.ApplyAsync(txContext);
                
                Logger.LogTrace($"Method: {singleTxExecutingDto.Transaction.MethodName}, " +
                                            $"Call Count: {trace.ExecutionCallCount}, " +
                                            $"Branch Count: {trace.ExecutionBranchCount}");

                if (txContext.Trace.IsSuccessful())
                    await ExecuteInlineTransactions(singleTxExecutingDto.Depth, singleTxExecutingDto.CurrentBlockTime,
                        txContext, internalStateCache,
                        internalChainContext, cancellationToken);

                #region PostTransaction

                if (singleTxExecutingDto.Depth == 0)
                {
                    if (!await ExecutePluginOnPostTransactionStageAsync(executive, txContext, singleTxExecutingDto.CurrentBlockTime,
                        internalChainContext, internalStateCache, cancellationToken))
                    {
                        trace.ExecutionStatus = ExecutionStatus.Postfailed;
                        return trace;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Transaction execution failed.");
                txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                txContext.Trace.Error += ex + "\n";
                throw;
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(singleTxExecutingDto.Transaction.To, executive);
#if DEBUG
                await LocalEventBus.PublishAsync(new TransactionExecutedEventData
                {
                    TransactionTrace = trace
                });
#endif
            }

            return trace;
        }

        private async Task ExecuteInlineTransactions(int depth, Timestamp currentBlockTime,
            ITransactionContext txContext, TieredStateCache internalStateCache,
            IChainContext internalChainContext, CancellationToken cancellationToken)
        {
            var trace = txContext.Trace;
            internalStateCache.Update(txContext.Trace.GetStateSets());
            foreach (var inlineTx in txContext.Trace.InlineTransactions)
            {
                var singleTxExecutingDto = new SingleTransactionExecutingDto
                {
                    Depth = depth + 1,
                    ChainContext = internalChainContext,
                    Transaction = inlineTx,
                    CurrentBlockTime = currentBlockTime,
                    Origin = txContext.Origin
                };

                // Only system contract can send TransferFrom tx as inline tx.
                if (!_inlineTransactionValidationService.Validate(inlineTx))
                    break;

                var inlineTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);

                if (inlineTrace == null)
                    break;
                trace.InlineTraces.Add(inlineTrace);
                if (!inlineTrace.IsSuccessful())
                {
                    // Already failed, no need to execute remaining inline transactions
                    break;
                }

                internalStateCache.Update(inlineTrace.GetStateSets());
            }
        }

        private async Task<bool> ExecutePluginOnPreTransactionStageAsync(IExecutive executive,
            ITransactionContext txContext,
            Timestamp currentBlockTime,
            IChainContext internalChainContext,
            TieredStateCache internalStateCache,
            CancellationToken cancellationToken)
        {
            var trace = txContext.Trace;
            foreach (var plugin in _prePlugins)
            {
                var transactions = await plugin.GetPreTransactionsAsync(executive.Descriptors, txContext);
                foreach (var preTx in transactions)
                {
                    var singleTxExecutingDto = new SingleTransactionExecutingDto
                    {
                        Depth = 0,
                        ChainContext = internalChainContext,
                        Transaction = preTx,
                        CurrentBlockTime = currentBlockTime
                    };
                    var preTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
                    if (preTrace == null)
                        return false;
                    trace.PreTransactions.Add(preTx);
                    trace.PreTraces.Add(preTrace);
                    if (preTx.MethodName == "ChargeTransactionFees")
                    {
                        var txFee = new TransactionFee();
                        txFee.MergeFrom(preTrace.ReturnValue);
                        trace.TransactionFee = txFee;
                    }
                    
                    if (!preTrace.IsSuccessful())
                    {
                        return false;
                    }

                    var stateSets = preTrace.GetStateSets().ToList();
                    internalStateCache.Update(stateSets);
                    var parentStateCache = txContext.StateCache as TieredStateCache;
                    parentStateCache?.Update(stateSets);

                    if (trace.TransactionFee == null || !trace.TransactionFee.IsFailedToCharge) continue;

                    preTrace.ExecutionStatus = ExecutionStatus.Executed;
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ExecutePluginOnPostTransactionStageAsync(IExecutive executive,
            ITransactionContext txContext,
            Timestamp currentBlockTime,
            IChainContext internalChainContext,
            TieredStateCache internalStateCache,
            CancellationToken cancellationToken)
        {
            var trace = txContext.Trace;
            if (!trace.IsSuccessful())
            {
                internalStateCache = new TieredStateCache(txContext.StateCache);
                foreach (var preTrace in txContext.Trace.PreTraces)
                {
                    var stateSets = preTrace.GetStateSets();
                    internalStateCache.Update(stateSets);
                }

                internalChainContext.StateCache = internalStateCache;
            }
            
            foreach (var plugin in _postPlugins)
            {
                var transactions = await plugin.GetPostTransactionsAsync(executive.Descriptors, txContext);
                foreach (var postTx in transactions)
                {
                    var singleTxExecutingDto = new SingleTransactionExecutingDto
                    {
                        Depth = 0,
                        ChainContext = internalChainContext,
                        Transaction = postTx,
                        CurrentBlockTime = currentBlockTime
                    };
                    var postTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
                    
                    if (postTrace == null)
                        return false;
                    trace.PostTransactions.Add(postTx);
                    trace.PostTraces.Add(postTrace);

                    if (postTx.MethodName == "ChargeResourceToken")
                    {
                        var consumedResourceTokens = new ConsumedResourceTokens();
                        consumedResourceTokens.MergeFrom(postTrace.ReturnValue);
                        trace.ConsumedResourceTokens = consumedResourceTokens;
                    }

                    if (!postTrace.IsSuccessful())
                    {
                        return false;
                    }

                    internalStateCache.Update(postTrace.GetStateSets());
                }
            }

            return true;
        }

        private TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            if (trace.ExecutionStatus == ExecutionStatus.Undefined)
            {
                return new TransactionResult
                {
                    TransactionId = trace.TransactionId,
                    Status = TransactionResultStatus.Unexecutable,
                    BlockNumber = blockHeight,
                    Error = ExecutionStatus.Undefined.ToString()
                };
            }

            if (trace.ExecutionStatus == ExecutionStatus.Prefailed)
            {
                if (trace.TransactionFee != null && trace.TransactionFee.IsFailedToCharge)
                {
                    return new TransactionResult
                    {
                        TransactionId = trace.TransactionId,
                        Status = TransactionResultStatus.Failed,
                        ReturnValue = trace.ReturnValue,
                        ReadableReturnValue = trace.ReadableReturnValue,
                        BlockNumber = blockHeight,
                        Logs = {trace.FlattenedLogs},
                        Error = ExecutionStatus.InsufficientTransactionFees.ToString()
                    };
                }
                return new TransactionResult
                {
                    TransactionId = trace.TransactionId,
                    Status = TransactionResultStatus.Unexecutable,
                    BlockNumber = blockHeight,
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
                BlockNumber = blockHeight,
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
                var transactionExecutingStateSets = trace.GetStateSets();
                returnSet = GetReturnSet(returnSet, transactionExecutingStateSets);
                returnSet.ReturnValue = trace.ReturnValue;
            }
            else
            {
                var transactionExecutingStateSets = new List<TransactionExecutingStateSet>();
                foreach (var preTrace in trace.PreTraces)
                {
                    if (preTrace.IsSuccessful()) transactionExecutingStateSets.AddRange(preTrace.GetStateSets());
                }
                    
                foreach (var postTrace in trace.PostTraces)
                {
                    if (postTrace.IsSuccessful()) transactionExecutingStateSets.AddRange(postTrace.GetStateSets());
                }

                returnSet = GetReturnSet(returnSet, transactionExecutingStateSets);
            }

            var reads = trace.GetFlattenedReads();
            foreach (var read in reads)
            {
                returnSet.StateAccesses[read.Key] = read.Value;
            }

            return returnSet;
        }
        
        private ExecutionReturnSet GetReturnSet(ExecutionReturnSet returnSet,
            IEnumerable<TransactionExecutingStateSet> transactionExecutingStateSets)
        {
            foreach (var transactionExecutingStateSet in transactionExecutingStateSets)
            {
                foreach (var write in transactionExecutingStateSet.Writes)
                {
                    returnSet.StateChanges[write.Key] = write.Value;
                    returnSet.StateDeletes.Remove(write.Key);
                }
                
                foreach (var delete in transactionExecutingStateSet.Deletes)
                {
                    returnSet.StateDeletes[delete.Key] = delete.Value;
                    returnSet.StateChanges.Remove(delete.Key);
                }
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

        private TransactionContext CreateTransactionContext(SingleTransactionExecutingDto singleTxExecutingDto,
            out TransactionTrace trace)
        {
            if (singleTxExecutingDto.Transaction.To == null || singleTxExecutingDto.Transaction.From == null)
            {
                throw new Exception($"error tx: {singleTxExecutingDto.Transaction}");
            }

            trace = new TransactionTrace
            {
                TransactionId = singleTxExecutingDto.Transaction.GetHash()
            };
            var txContext = new TransactionContext
            {
                PreviousBlockHash = singleTxExecutingDto.ChainContext.BlockHash,
                CurrentBlockTime = singleTxExecutingDto.CurrentBlockTime,
                Transaction = singleTxExecutingDto.Transaction,
                BlockHeight = singleTxExecutingDto.ChainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = singleTxExecutingDto.Depth,
                StateCache = singleTxExecutingDto.ChainContext.StateCache,
                Origin = singleTxExecutingDto.Origin != null
                    ? singleTxExecutingDto.Origin
                    : singleTxExecutingDto.Transaction.From
            };

            return txContext;
        }
    }
}