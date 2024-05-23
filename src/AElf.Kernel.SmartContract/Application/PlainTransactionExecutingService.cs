using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application;

public class PlainTransactionExecutingService : IPlainTransactionExecutingService, ISingletonDependency
{
    private readonly List<IPostExecutionPlugin> _postPlugins;
    private readonly List<IPreExecutionPlugin> _prePlugins;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ITransactionContextFactory _transactionContextFactory;
    private readonly IFeatureDisableService _featureDisableService;

    public PlainTransactionExecutingService(ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory, IFeatureDisableService featureDisableService)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _transactionContextFactory = transactionContextFactory;
        _featureDisableService = featureDisableService;
        _prePlugins = GetUniquePlugins(prePlugins);
        _postPlugins = GetUniquePlugins(postPlugins);
        Logger = NullLogger<PlainTransactionExecutingService>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public ILogger<PlainTransactionExecutingService> Logger { get; set; }

    public ILocalEventBus LocalEventBus { get; set; }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var groupStateCache = transactionExecutingDto.PartialBlockStateSet.ToTieredStateCache();
            var groupChainContext = new ChainContextWithTieredStateCache(
                transactionExecutingDto.BlockHeader.PreviousBlockHash,
                transactionExecutingDto.BlockHeader.Height - 1, groupStateCache);

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
                    OriginTransactionId = transaction.GetHash()
                };
                try
                {
                    var transactionExecutionTask = Task.Run(() => ExecuteOneAsync(singleTxExecutingDto,
                        cancellationToken), cancellationToken);

                    trace = await transactionExecutionTask.WithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogTrace("Transaction canceled");
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    continue;
                }


                if (!TryUpdateStateCache(trace, groupStateCache))
                    break;
#if DEBUG
                if (!string.IsNullOrEmpty(trace.Error)) Logger.LogInformation("{Error}", trace.Error);
#endif
                var result = GetTransactionResult(trace, transactionExecutingDto.BlockHeader.Height);

                var returnSet = GetReturnSet(trace, result);
                returnSets.Add(returnSet);
            }

            return returnSets;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed while executing txs in block");
            throw;
        }
    }

    private static bool TryUpdateStateCache(TransactionTrace trace, TieredStateCache groupStateCache)
    {
        if (trace == null)
            return false;

        if (!trace.IsSuccessful())
        {
            var transactionExecutingStateSets = new List<TransactionExecutingStateSet>();

            AddToTransactionStateSets(transactionExecutingStateSets, trace.PreTraces);
            AddToTransactionStateSets(transactionExecutingStateSets, trace.PostTraces);

            groupStateCache.Update(transactionExecutingStateSets);
            trace.SurfaceUpError();
        }
        else
        {
            groupStateCache.Update(trace.GetStateSets());
        }

        return true;
    }

    private static void AddToTransactionStateSets(List<TransactionExecutingStateSet> transactionExecutingStateSets,
        RepeatedField<TransactionTrace> traces)
    {
        transactionExecutingStateSets.AddRange(traces.Where(p => p.IsSuccessful())
            .SelectMany(p => p.GetStateSets()));
    }

    protected virtual async Task<TransactionTrace> ExecuteOneAsync(
        SingleTransactionExecutingDto singleTxExecutingDto,
        CancellationToken cancellationToken)
    {
        if (singleTxExecutingDto.IsCancellable)
            cancellationToken.ThrowIfCancellationRequested();

        var txContext = CreateTransactionContext(singleTxExecutingDto);
        var trace = txContext.Trace;

        var internalStateCache = new TieredStateCache(singleTxExecutingDto.ChainContext.StateCache);
        var internalChainContext =
            new ChainContextWithTieredStateCache(singleTxExecutingDto.ChainContext, internalStateCache);

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
                if (!await ExecutePluginOnPreTransactionStageAsync(executive, txContext,
                        singleTxExecutingDto.CurrentBlockTime,
                        internalChainContext, internalStateCache, cancellationToken))
                {
                    trace.ExecutionStatus = ExecutionStatus.Prefailed;
                    return trace;
                }

            #endregion

            await executive.ApplyAsync(txContext);

            if (txContext.Trace.IsSuccessful())
                await ExecuteInlineTransactions(singleTxExecutingDto.Depth, singleTxExecutingDto.CurrentBlockTime,
                    txContext, internalStateCache,
                    internalChainContext,
                    singleTxExecutingDto.OriginTransactionId,
                    cancellationToken);

            #region PostTransaction

            if (singleTxExecutingDto.Depth == 0)
                if (!await ExecutePluginOnPostTransactionStageAsync(executive, txContext,
                        singleTxExecutingDto.CurrentBlockTime,
                        internalChainContext, internalStateCache, cancellationToken))
                {
                    trace.ExecutionStatus = ExecutionStatus.Postfailed;
                    return trace;
                }

            #endregion
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Transaction execution failed");
            txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
            txContext.Trace.Error += ex + "\n";
            throw;
        }
        finally
        {
            await _smartContractExecutiveService.PutExecutiveAsync(singleTxExecutingDto.ChainContext,
                singleTxExecutingDto.Transaction.To, executive);
        }

        return trace;
    }

    private async Task ExecuteInlineTransactions(int depth, Timestamp currentBlockTime,
        ITransactionContext txContext, TieredStateCache internalStateCache,
        IChainContext internalChainContext,
        Hash originTransactionId,
        CancellationToken cancellationToken)
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
                Origin = txContext.Origin,
                OriginTransactionId = originTransactionId
            };

            var inlineTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);

            if (inlineTrace == null)
                break;
            trace.InlineTraces.Add(inlineTrace);
            if (!inlineTrace.IsSuccessful())
                // Already failed, no need to execute remaining inline transactions
                break;

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
        if (await _featureDisableService.IsFeatureDisabledAsync("TxPlugin", "PrePlugin"))
        {
            return true;
        }

        var trace = txContext.Trace;
        foreach (var plugin in _prePlugins)
        {
            var transactions = await plugin.GetPreTransactionsAsync(executive.Descriptors, txContext);
            foreach (var preTx in transactions)
            {
                var singleTxExecutingDto = new SingleTransactionExecutingDto
                {
                    Depth = 0, //TODO: this 0 means it is possible that pre/post txs could have own pre/post txs
                    ChainContext = internalChainContext,
                    Transaction = preTx,
                    CurrentBlockTime = currentBlockTime,
                    OriginTransactionId = txContext.OriginTransactionId
                };
                var preTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
                if (preTrace == null)
                    return false;
                trace.PreTransactions.Add(preTx);
                trace.PreTraces.Add(preTrace);

                if (!preTrace.IsSuccessful()) return false;

                var stateSets = preTrace.GetStateSets().ToList();
                internalStateCache.Update(stateSets);
                var parentStateCache = txContext.StateCache as TieredStateCache;
                parentStateCache?.Update(stateSets);

                if (!plugin.IsStopExecuting(preTrace.ReturnValue, out var error)) continue;

                // If pre-tx fails, still commit the changes, but return false to notice outside to stop the execution.
                preTrace.Error = error;
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
        if (await _featureDisableService.IsFeatureDisabledAsync("TxPlugin", "PostPlugin"))
        {
            return true;
        }

        var trace = txContext.Trace;

        if (!trace.IsSuccessful())
        {
            // If failed to execute this tx, at least we need to commit pre traces.
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
                    CurrentBlockTime = currentBlockTime,
                    OriginTransactionId = txContext.OriginTransactionId
                };
                var postTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);

                if (postTrace == null)
                    return false;
                trace.PostTransactions.Add(postTx);
                trace.PostTraces.Add(postTrace);

                if (!postTrace.IsSuccessful()) return false;

                internalStateCache.Update(postTrace.GetStateSets());
            }
        }

        return true;
    }

    private TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
    {
        var txResult = new TransactionResult
        {
            TransactionId = trace.TransactionId,
            BlockNumber = blockHeight
        };

        if (!trace.IsSuccessful())
        {
            // Is failed.
            txResult.Status = TransactionResultStatus.Failed;
            if (trace.ExecutionStatus == ExecutionStatus.Undefined)
            {
                // Cannot find specific contract method.
                txResult.Error = ExecutionStatus.Undefined.ToString();
            }
            else
            {
                txResult.Error = trace.Error;
                if (trace.ExecutionStatus != ExecutionStatus.Prefailed || IsExecutionStoppedByPrePlugin(trace))
                {
                    txResult.Logs.AddRange(trace.GetPluginLogs());
                    txResult.UpdateBloom();
                }
            }
        }
        else
        {
            // Is successful.
            txResult.Status = TransactionResultStatus.Mined;
            txResult.ReturnValue = trace.ReturnValue;
            txResult.Logs.AddRange(trace.FlattenedLogs);

            txResult.UpdateBloom();
        }

        return txResult;
    }

    private ExecutionReturnSet GetReturnSet(TransactionTrace trace, TransactionResult result)
    {
        var returnSet = new ExecutionReturnSet
        {
            TransactionId = result.TransactionId,
            Status = result.Status,
            Bloom = result.Bloom,
            TransactionResult = result
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
                if (preTrace.IsSuccessful())
                    transactionExecutingStateSets.AddRange(preTrace.GetStateSets());

            foreach (var postTrace in trace.PostTraces)
                if (postTrace.IsSuccessful())
                    transactionExecutingStateSets.AddRange(postTrace.GetStateSets());

            returnSet = GetReturnSet(returnSet, transactionExecutingStateSets);
        }

        var reads = trace.GetFlattenedReads();
        foreach (var read in reads) returnSet.StateAccesses[read.Key] = read.Value;

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

    private static List<T> GetUniquePlugins<T>(IEnumerable<T> plugins)
    {
        // One instance per type
        return plugins.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
    }

    private bool IsExecutionStoppedByPrePlugin(TransactionTrace trace)
    {
        return _prePlugins.Any(prePlugin =>
            trace.PreTraces.Any(preTrace => prePlugin.IsStopExecuting(preTrace.ReturnValue, out _)));
    }

    protected ITransactionContext CreateTransactionContext(SingleTransactionExecutingDto singleTxExecutingDto)
    {
        if (singleTxExecutingDto.Transaction.To == null || singleTxExecutingDto.Transaction.From == null)
            throw new Exception($"error tx: {singleTxExecutingDto.Transaction}");


        var origin = singleTxExecutingDto.Origin != null
            ? singleTxExecutingDto.Origin
            : singleTxExecutingDto.Transaction.From;

        var txContext = _transactionContextFactory.Create(singleTxExecutingDto.Transaction,
            singleTxExecutingDto.ChainContext, singleTxExecutingDto.OriginTransactionId, origin,
            singleTxExecutingDto.Depth, singleTxExecutingDto.CurrentBlockTime);

        return txContext;
    }
}