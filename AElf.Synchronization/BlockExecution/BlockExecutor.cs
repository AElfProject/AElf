using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.Domain;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.Services;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Synchronization.BlockExecution
{
    public class BlockExecutor : IBlockExecutor
    {
        private readonly IChainService _chainService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IExecutingService _executingService;
        public ILogger<BlockExecutor> Logger {get;set;}
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly ITxHub _txHub;
        private readonly IStateManager _stateManager;
        private readonly ConsensusDataProvider _consensusDataProvider;
        private static bool _isLimitExecutionTime;

        private const float RatioSynchronize = 0.5f;

        public BlockExecutor(IChainService chainService, IExecutingService executingService,
            ITransactionResultManager transactionResultManager, IBinaryMerkleTreeManager binaryMerkleTreeManager, 
            ITxHub txHub, IStateManager stateManager, ConsensusDataProvider consensusDataProvider)
        {
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _txHub = txHub;
            _stateManager = stateManager;
            _consensusDataProvider = consensusDataProvider;

            Logger= NullLogger<BlockExecutor>.Instance;

            MessageHub.Instance.Subscribe<DPoSStateChanged>(inState => _isMining = inState.IsMining);

            MessageHub.Instance.Subscribe<StateEvent>(inState =>
            {
                if (inState == StateEvent.RollbackFinished)
                {
                    _isLimitExecutionTime = false;
                }

                if (inState == StateEvent.MiningStart)
                {
                    _isLimitExecutionTime = true;
                }

                Logger.LogTrace($"Current Event: {inState.ToString()}, IsLimitExecutionTime: {_isLimitExecutionTime}.");
            });
        }

        private static bool _isMining;

        /// <inheritdoc/>
        public async Task<BlockExecutionResult> ExecuteBlock(IBlock block)
        {
            if (_isMining)
            {
                Logger.LogTrace($"Prevent block {block.BlockHashToHex} from entering block execution," +
                               "for this node is doing mining.");
                return BlockExecutionResult.Mining;
            }

            var result = Prepare(block);
            if (result.IsFailed())
            {
                return result;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var txnRes = new List<TransactionResult>();
            var cts = new CancellationTokenSource();

            var res = BlockExecutionResult.Fatal;
            try
            {
                double timeout = 0;
                if (_isLimitExecutionTime)
                {
                    var distanceToTimeSlot = await _consensusDataProvider.GetDistanceToTimeSlotEnd(block.Header.ChainId);
                    timeout = distanceToTimeSlot * RatioSynchronize;
                    cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                }

                // 1. Collection result.
                // 2. Transaction for indexing side chain block, if exists. 
                res = await TryCollectTransactions(block, cts);
                if (result.IsFailed())
                {
                    Logger.LogWarning(
                        $"Collect transaction from block failed: {result}, block height: {block.Header.Height}, " +
                        $"block hash: {block.BlockHashToHex}.");
                    res = result;
                    return res;
                }

                var readyTxs = block.Body.TransactionList.ToList();
                var traces = await ExecuteTransactions(readyTxs, block.Header.ChainId,
                block.Header.Time.ToDateTime(), block.Header.GetDisambiguationHash(), cts);

                // Execute transactions.
                // After this, rollback needed
                if ((res = ExtractTransactionResults(block.Header.ChainId, traces, out txnRes)).IsFailed())
                {
                    throw new InvalidBlockException(res.ToString());
                }

                if (cts.IsCancellationRequested)
                {
                    Logger.LogTrace(
                        $"Execution cancelled and rollback: block hash: {block.BlockHashToHex}, " +
                        $"execution time: {timeout} ms.");
                    res = BlockExecutionResult.ExecutionCancelled;
                    throw new InvalidBlockException("Block execution timeout");
                }

                txnRes = SortToOriginalOrder(txnRes, readyTxs);

                if ((result = UpdateWorldState(block, txnRes)).IsFailed())
                {
                    res = result;
                    throw new InvalidBlockException(result.ToString());
                }

                // BlockExecuting -> BlockAppending
                // ExecutingLoop -> BlockAppending
                MessageHub.Instance.Publish(StateEvent.StateUpdated);

                await AppendBlock(block);
                await InsertTxs(txnRes, block);
                await _txHub.OnNewBlock((Block) block);

                res = BlockExecutionResult.Success;
                return res;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Exception while execute block {block.BlockHashToHex}.");
                // TODO, no wait may need improve
                var task = Rollback(block, txnRes);

                return res;
            }
            finally
            {
                cts.Dispose();
                stopwatch.Stop();

                Logger.LogInformation($"Executed block {block.BlockHashToHex} with result {res}, " +
                              $"{block.Body.Transactions.Count} txns, duration {stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        /// <summary>
        /// Execute transactions.
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="chainId"></param>
        /// <param name="disambiguationHash"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <param name="toDateTime"></param>
        /// <returns></returns>
        private async Task<List<TransactionTrace>> ExecuteTransactions(List<Transaction> readyTxs, int chainId,
            DateTime toDateTime, Hash disambiguationHash, CancellationTokenSource cancellationTokenSource)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, chainId, toDateTime, cancellationTokenSource.Token,
                    disambiguationHash);
            return traces;
        }

        private BlockExecutionResult ExtractTransactionResults(int chainId, List<TransactionTrace> traces, 
            out List<TransactionResult> results)
        {
            results = new List<TransactionResult>();
            int index = 0;
            foreach (var trace in traces)
            {
                // Todo : This can be extracted out since it has to be consistent with miner processing.
                switch (trace.ExecutionStatus)
                {
                     case ExecutionStatus.ExecutedAndCommitted:
                        // Successful
                        var txRes = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            Status = TransactionResultStatus.Mined,
                            RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes()),
                            StateHash = trace.GetSummarizedStateHash(),
                            Index = index++
                        };
                        txRes.UpdateBloom();
                        
                        // insert deferred txn to transaction pool and wait for execution 
                        if (trace.DeferredTransaction.Length != 0)
                        {
                            var deferredTxn = Transaction.Parser.ParseFrom(trace.DeferredTransaction);
                            _txHub.AddTransactionAsync(chainId, deferredTxn).ConfigureAwait(false);
                            txRes.DeferredTxnId = deferredTxn.GetHash();
                        }

                        results.Add(txRes);
                        break;
                    case ExecutionStatus.ContractError:
                        var txResF = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            RetVal = ByteString.CopyFromUtf8(trace.StdErr), // Is this needed?
                            Status = TransactionResultStatus.Failed,
                            StateHash = Hash.Default,
                            Index = index++
                        };
                        results.Add(txResF);
                        break;
                    case ExecutionStatus.InsufficientTransactionFees:
                        var txResITF = new TransactionResult
                        {
                            TransactionId = trace.TransactionId,
                            RetVal = ByteString.CopyFromUtf8(trace.ExecutionStatus.ToString()), // Is this needed?
                            Status = TransactionResultStatus.Failed,
                            StateHash = trace.GetSummarizedStateHash(),
                            Index = index++
                        };
                        results.Add(txResITF);
                        break;
                    /*case ExecutionStatus.Undefined:
                        break;
                    case ExecutionStatus.ExecutedButNotCommitted:
                        break;
                    case ExecutionStatus.Canceled:
                        break;
                    case ExecutionStatus.SystemError:
                        break;
                    case ExecutionStatus.ExceededMaxCallDepth:
                        break;*/
                    default:
                        Logger.LogTrace(
                            $"Transaction {trace.TransactionId} execution failed with status {trace.ExecutionStatus}");
                        break;
                }
            }
            return BlockExecutionResult.TransactionExecutionSuccess;
        }

        private List<TransactionResult> SortToOriginalOrder(List<TransactionResult> results, List<Transaction> txs)
        {
            var indexes = txs.Select((x, i) => new {hash = x.GetHash(), ind = i}).ToDictionary(x => x.hash, x => x.ind);
            return results.Zip(results.Select(r => indexes[r.TransactionId]), Tuple.Create).OrderBy(
                x => x.Item2).Select(x => x.Item1).ToList();
        }

        #region Before transaction execution

        /// <summary>
        /// Verify block components and validate side chain info if needed
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private BlockExecutionResult Prepare(IBlock block)
        {
            string errorLog = null;
            var res = BlockExecutionResult.PrepareSuccess;
            if (block?.Header == null)
            {
                errorLog = "ExecuteBlock - Block is null.";
                res = BlockExecutionResult.BlockIsNull;
            }
            else if (block.Body?.Transactions == null || block.Body.TransactionsCount <= 0)
            {
                errorLog = "ExecuteBlock - Transaction list is empty.";
                res = BlockExecutionResult.NoTransaction;
            }

            if (res.IsFailed())
                Logger.LogWarning(errorLog);

            return res;
        }

        /// <summary>
        /// Get txs from tx pool
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="block"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns>
        /// 1. Collection result.
        /// 2. Transaction receipts.
        /// 3. Transaction for indexing side chain block, if exists. 
        /// </returns>
        private async Task<BlockExecutionResult> TryCollectTransactions(IBlock block,
            CancellationTokenSource cancellationTokenSource)
        {
            var res = BlockExecutionResult.CollectTransactionsSuccess;
            var txs = block.Body.TransactionList.ToList();
            if (txs.Count == 0)
            {
                return BlockExecutionResult.NoTransaction;
            }

//            var noIndexingSideChainTransaction = true;
//            var noIndexingParentChainTransaction = true;
            foreach (var tx in txs)
            {

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    res = BlockExecutionResult.NotExecutable;
                    break;
                }

                var receipt = await _txHub.GetCheckedReceiptsAsync(block.Header.ChainId, tx);
                if (receipt.IsExecutable)
                    continue;
                res = BlockExecutionResult.NotExecutable;
                break;
            }

            return res;
        }


        #endregion

        #region After transaction execution

        /// <summary>
        /// Update system state.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private BlockExecutionResult UpdateWorldState(IBlock block, IEnumerable<TransactionResult> results)
        {
            var transactionResults = results.ToList();
            var root = new BinaryMerkleTree().AddNodes(transactionResults.Select(x => x.StateHash)).ComputeRootHash();
            var res = BlockExecutionResult.UpdateWorldStateSuccess;
            if (root != block.Header.MerkleTreeRootOfWorldState)
            {
                Logger.LogTrace($"{root.ToHex()} != {block.Header.MerkleTreeRootOfWorldState.ToHex()}");
                Logger.LogWarning("ExecuteBlock - Incorrect merkle trees.");
                Logger.LogTrace("Transaction Results:");
                foreach (var r in transactionResults)
                {
                    Logger.LogTrace($"TransactionId: {r.TransactionId.ToHex()}, " +
                                   $"StateHash: {r.StateHash.ToHex()}，" +
                                   $"Status: {r.Status}, " +
                                   $"{r.RetVal}");
                }

                res = BlockExecutionResult.IncorrectStateMerkleTree;
            }

            return res;
        }

        /// <summary>
        /// Append block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task AppendBlock(IBlock block)
        {
            var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
            await blockchain.AddBlocksAsync(new List<IBlock> {block});
        }

        /// <summary>
        /// Update database
        /// </summary>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private async Task InsertTxs(List<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Height;
            var bh = block.Header.GetHash();

            await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree,
                block.Header.ChainId, block.Header.Height);
            txResults.AsParallel().ToList().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
        }

        #endregion

        #region Rollback

        private async Task Rollback(IBlock block, IEnumerable<TransactionResult> txRes)
        {
            if (block == null)
                return;
            var blockChain = _chainService.GetBlockChain(block.Header.ChainId);
            await blockChain.RollbackStateForTransactions(
                txRes.Where(x => x.Status == TransactionResultStatus.Mined).Select(x => x.TransactionId),
                block.Header.GetDisambiguationHash()
            );
        }

        #endregion

    }

    internal class InvalidBlockException : Exception
    {
        public InvalidBlockException(string message) : base(message)
        {
        }
    }
}