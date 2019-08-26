using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public class TransactionExecutingService : ITransactionExecutingService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly List<IPreExecutionPlugin> _prePlugins;
        private readonly List<IPostExecutionPlugin> _postPlugins;
        private readonly ITransactionResultService _transactionResultService;
        public ILogger<TransactionExecutingService> Logger { get; set; }

        public TransactionExecutingService(ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService, IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins
            )
        {
            _transactionResultService = transactionResultService;
            _smartContractExecutiveService = smartContractExecutiveService;
            _prePlugins = GetUniquePrePlugins(prePlugins);
            _postPlugins = GetUniquePostPlugins(postPlugins);
            Logger = NullLogger<TransactionExecutingService>.Instance;
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
            foreach (var transaction in transactionExecutingDto.Transactions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                TransactionParameters parameters = new TransactionParameters(transaction, 
                groupChainContext, transactionExecutingDto.BlockHeader.Time);
                var trace = await ExecuteOneAsync(0, parameters, cancellationToken);

                // Will be useful when debugging MerkleTreeRootOfWorldState is different from each miner.
                //Logger.LogTrace(transaction.MethodName);
                //Logger.LogTrace(trace.StateSet.Writes.Values.Select(v => v.ToBase64().CalculateHash().ToHex()).JoinAsString("\n"));

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

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, TransactionParameters parameters, CancellationToken cancellationToken,
            Address origin = null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace
                {
                    TransactionId = parameters.Transaction.GetHash(),
                    ExecutionStatus = ExecutionStatus.Canceled,
                    Error = "Execution cancelled"
                };
            }

            if (parameters.Transaction.To == null || parameters.Transaction.From == null)
            {
                throw new Exception($"error tx: {parameters.Transaction}");
            }

            TransactionExcuteEntry excuteEntry = new TransactionExcuteEntry(
                depth,parameters,origin);

            var executive = await _smartContractExecutiveService.GetExecutiveAsync(
                excuteEntry.internalChainContext,
                excuteEntry.txCtxt.Transaction.To);
                
            try
            {                
                if(!await ProcessTransactionExecution(depth,executive,excuteEntry, cancellationToken))
                {
                    return excuteEntry.trace;
                }
            }
            catch (Exception ex)
            {
                excuteEntry.txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                excuteEntry.txCtxt.Trace.Error += ex + "\n";
                throw;
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(excuteEntry.txCtxt.Transaction.To, executive);
            }

            return excuteEntry.trace;
        }

        private async Task<bool> ProcessTransactionExecution(int depth, IExecutive executive,
         TransactionExcuteEntry excuteEntry, CancellationToken cancellationToken)
        {
            #region PreTransaction
            if (depth == 0)
            {
                if (!await ExecutePluginOnPreTransactionStageAsync(executive, excuteEntry.txCtxt, excuteEntry.txCtxt.CurrentBlockTime,
                    excuteEntry.internalChainContext, excuteEntry.internalStateCache, cancellationToken))
                {
                    return false;
                }
            }
            #endregion
            await executive.ApplyAsync(excuteEntry.txCtxt);

            await ExecuteInlineTransactions(depth, excuteEntry.txCtxt.CurrentBlockTime, excuteEntry.txCtxt, excuteEntry.internalStateCache,
                excuteEntry.internalChainContext, cancellationToken);

            return true;
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
                    TransactionParameters parameters = new TransactionParameters(inlineTx, 
                    internalChainContext, currentBlockTime);
                    var inlineTrace = await ExecuteOneAsync(depth + 1, parameters, cancellationToken, txCtxt.Origin);
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
                var transactions = await plugin.GetPreTransactionsAsync(executive.Descriptors, txCtxt);
                foreach (var preTx in transactions)
                {
                    TransactionParameters parameters = new  TransactionParameters(preTx,
                     internalChainContext, currentBlockTime);
                    var preTrace = await ExecuteOneAsync(0, parameters, cancellationToken);
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
                var transactions = await plugin.GetPostTransactionsAsync(executive.Descriptors, txCtxt);
                foreach (var postTx in transactions)
                {
                    var postTrace = await ExecuteOneAsync(0, internalChainContext, postTx, currentBlockTime,
                        cancellationToken);
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

    class TransactionExcuteEntry
    {
        public TransactionTrace trace {get; set;}
        public TransactionContext txCtxt {get; set;}
        public TieredStateCache internalStateCache {get; set;}
        public ChainContextWithTieredStateCache internalChainContext {get; set;}
        public TransactionExcuteEntry(int depth, TransactionParameters parameters, Address origin = null)
        {
             this.trace = new TransactionTrace
            {
                TransactionId = parameters.Transaction.GetHash()
            };

            this.txCtxt = new TransactionContext
            {
                PreviousBlockHash = parameters.ChainContext.BlockHash,
                CurrentBlockTime = parameters.CurrentBlockTime,
                Transaction = parameters.Transaction,
                BlockHeight = parameters.ChainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = depth,
                StateCache = parameters.ChainContext.StateCache,
                Origin = origin != null ? origin : parameters.Transaction.From
            };
            
            this.internalStateCache = new TieredStateCache(parameters.ChainContext.StateCache);
            this.internalChainContext = new ChainContextWithTieredStateCache(parameters.ChainContext, internalStateCache);
        }

    }
    class TransactionParameters
    {
        public Transaction Transaction { get; set; }
        public IChainContext ChainContext { get; set; }
        public Timestamp CurrentBlockTime { get; set; }

        public TransactionParameters(Transaction transaction, IChainContext chainContext, Timestamp currentBlockTime)
        {
            this.Transaction = transaction;
            this.ChainContext = chainContext;
            this.CurrentBlockTime = currentBlockTime;
        }
    }
}