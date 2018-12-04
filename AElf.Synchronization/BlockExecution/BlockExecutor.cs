using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.Types.Common;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using AElf.Miner.TxMemPool;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using NLog;
using NServiceKit.Common.Extensions;

namespace AElf.Synchronization.BlockExecution
{
    public class BlockExecutor : IBlockExecutor
    {
        private readonly IChainService _chainService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IExecutingService _executingService;
        private readonly ILogger _logger;
        private readonly ClientManager _clientManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly ITxHub _txHub;
        private readonly IChainManagerBasic _chainManagerBasic;
        private readonly IStateStore _stateStore;
        private readonly DPoSInfoProvider _dpoSInfoProvider;

        private static bool _executing;
        private static bool _prepareTerminated;
        private static bool _terminated;

        private static bool _isLimitExecutionTime;

        public BlockExecutor(IChainService chainService, IExecutingService executingService,
            ITransactionResultManager transactionResultManager, ClientManager clientManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ITxHub txHub, IChainManagerBasic chainManagerBasic, IStateStore stateStore)
        {
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _txHub = txHub;
            _chainManagerBasic = chainManagerBasic;
            _stateStore = stateStore;
            _dpoSInfoProvider = new DPoSInfoProvider(_stateStore);

            _logger = LogManager.GetLogger(nameof(BlockExecutor));

            MessageHub.Instance.Subscribe<DPoSStateChanged>(inState => _isMining = inState.IsMining);

            _executing = false;
            _prepareTerminated = false;
            _terminated = false;

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.BlockExecutor)
                {
                    if (!_executing)
                    {
                        _terminated = true;
                        MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockExecutor));
                    }
                    else
                    {
                        _prepareTerminated = true;
                    }
                }
            });

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

                _logger?.Trace($"Current Event: {inState.ToString()} ,IsLimitExecutionTime: {_isLimitExecutionTime}");
            });
        }

        private string _current;

        private static bool _isMining;

        /// <inheritdoc/>
        public async Task<BlockExecutionResult> ExecuteBlock(IBlock block)
        {
            if (_isMining)
            {
                _logger?.Trace($"Prevent block {block.BlockHashToHex} from entering block execution," +
                               "for this node is doing mining.");
                return BlockExecutionResult.Mining;
            }

            _current = block.BlockHashToHex;

            var result = Prepare(block);
            if (result.IsFailed())
            {
                _current = null;
                return result;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _executing = true;
            if (_terminated)
            {
                return BlockExecutionResult.Terminated;
            }

            var txnRes = new List<TransactionResult>();
            var readyTxs = new List<Transaction>();
            var cts = new CancellationTokenSource();

            var res = BlockExecutionResult.Fatal;
            try
            {
                // get txn from pool
                var tuple = CollectTransactions(block);
                result = tuple.Item1;
                readyTxs = tuple.Item2;
                if (result.IsFailed() || readyTxs.Count == 0)
                {
                    _logger?.Warn($"Collect transaction from block failed: {result}, block height: {block.Header.Index}, " +
                                  $"block hash: {block.BlockHashToHex}.");
                    res = result;
                    return res;
                }

                double distanceToTimeSlot = 0;
                if (_isLimitExecutionTime)
                {
                    distanceToTimeSlot = await _dpoSInfoProvider.GetDistanceToTimeSlotEnd();
                    cts.CancelAfter(TimeSpan.FromMilliseconds(distanceToTimeSlot * NodeConfig.Instance.RatioSynchronize));
                }

                var trs = await _txHub.GetReceiptsForAsync(readyTxs, cts);
                
                if (cts.IsCancellationRequested)
                {
                    return BlockExecutionResult.ExecutionCancelled;
                }

                foreach (var tr in trs)
                {
                    if (!tr.IsExecutable)
                    {
                        throw new InvalidBlockException($"Transaction is not executable, transaction: {tr}, " +
                                                        $"block height: {block.Header.Index}, block hash: {block.BlockHashToHex}, SignatureSt:{tr.SignatureSt},RefBlockSt:{tr.RefBlockSt},Status:{tr.Status}");
                    }
                }

                txnRes = await ExecuteTransactions(readyTxs, block.Header.ChainId, block.Header.GetDisambiguationHash(),cts);
                
                if (cts.IsCancellationRequested)
                {
                    _logger?.Trace($"Execution Cancelled and rollback: block hash: {block.BlockHashToHex}, execution time: {distanceToTimeSlot * NodeConfig.Instance.RatioSynchronize} ms.");
                    Rollback(block, txnRes).ConfigureAwait(false);
                    return BlockExecutionResult.ExecutionCancelled;
                }
                
                txnRes = SortToOriginalOrder(txnRes, readyTxs);

                var blockChain = _chainService.GetBlockChain(Hash.LoadHex(ChainConfig.Instance.ChainId));
                if (await blockChain.GetBlockByHashAsync(block.GetHash()) != null)
                {
                    res = BlockExecutionResult.AlreadyAppended;
                    return res;
                }

                result = UpdateWorldState(block, txnRes);
                if (result.IsFailed())
                {
                    res = result;
                    return res;
                }

                await UpdateCrossChainInfo(block, txnRes);

                // BlockExecuting -> BlockAppending
                // ExecutingLoop -> BlockAppending
                MessageHub.Instance.Publish(StateEvent.StateUpdated);

                await AppendBlock(block);
                InsertTxs(txnRes, block);

                await _txHub.OnNewBlock((Block) block);

                res = BlockExecutionResult.Success;
                return res;
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Exception while execute block {block.BlockHashToHex}.");
                // TODO, no wait may need improve
                Rollback(block, txnRes).ConfigureAwait(false);

                return res;
            }
            finally
            {
                _current = null;
                _executing = false;
                cts.Dispose();
                if (_prepareTerminated)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockExecutor));
                }

                stopwatch.Stop();
                if (res.CanExecuteAgain())
                {
                    _logger?.Warn($"Block {block.BlockHashToHex} can execute again.");
                }

                _logger?.Info($"Executed block {block.BlockHashToHex} with result {res}, {block.Body.Transactions.Count} txns, " +
                              $"duration {stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        /// <summary>
        /// Execute transactions.
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="chainId"></param>
        /// <param name="disambiguationHash"></param>
        /// <returns></returns>
        private async Task<List<TransactionResult>> ExecuteTransactions(List<Transaction> readyTxs, Hash chainId,
            Hash disambiguationHash,CancellationTokenSource cancellationTokenSource)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, chainId, cancellationTokenSource.Token, disambiguationHash);

            var results = new List<TransactionResult>();
            foreach (var trace in traces)
            {
                var res = new TransactionResult
                {
                    TransactionId = trace.TransactionId
                };
                if (string.IsNullOrEmpty(trace.StdErr))
                {
                    res.Logs.AddRange(trace.FlattenedLogs);
                    res.Status = Status.Mined;
                    res.RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes());
                    res.StateHash = trace.GetSummarizedStateHash();
                }
                else
                {
                    res.Status = Status.Failed;
                    res.RetVal = ByteString.CopyFromUtf8(trace.StdErr);
                    res.StateHash = trace.GetSummarizedStateHash();
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger?.Error($"Transaction execute failed. TransactionId: {res.TransactionId.DumpHex()}, " +
                                       $"StateHash: {res.StateHash} Transaction deatils: {readyTxs.Find(x => x.GetHash() == trace.TransactionId)}" +
                                       $"\n {trace.StdErr}");
                    }
                }

                results.Add(res);
            }

            return results;
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
            else if (!ValidateSideChainBlockInfo(block))
            {
                // side chain info verification 
                // side chain info in this block cannot fit together with local side chain info.
                errorLog = "Invalid side chain info";
                res = BlockExecutionResult.InvalidSideChainInfo;
            }

            if (res.IsFailed())
                _logger?.Warn(errorLog);

            return res;
        }

        /// <summary>
        /// Check side chain info.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>
        /// Return true if side chain info is consistent with local node, else return false;
        /// </returns>
        private bool ValidateSideChainBlockInfo(IBlock block)
        {
            return block.Body.IndexedInfo.All(_clientManager.TryGetSideChainBlockInfo);
        }

        /// <summary>
        /// Get txs from tx pool
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private Tuple<BlockExecutionResult, List<Transaction>> CollectTransactions(IBlock block)
        {
            var res = BlockExecutionResult.CollectTransactionsSuccess;
            var txs = block.Body.TransactionList.ToList();
            var readyTxs = new List<Transaction>();
            foreach (var tx in txs)
            {
                if (tx.Type == TransactionType.CrossChainBlockInfoTransaction)
                {
                    // todo: verify transaction from address
                    var parentBlockInfo = (ParentChainBlockInfo) ParamsPacker.Unpack(tx.Params.ToByteArray(),
                        new[] {typeof(ParentChainBlockInfo)})[0];
                    if (!ValidateParentChainBlockInfoTransaction(parentBlockInfo))
                    {
                        //errorLog = "Invalid parent chain block info.";
                        res = BlockExecutionResult.InvalidParentChainBlockInfo;
                        break;
                    }

                    // for update
                    //block.ParentChainBlockInfo = parentBlockInfo;
                }

                readyTxs.Add(tx);
            }

            if (res.IsSuccess() && readyTxs.Count(t => t.Type == TransactionType.CrossChainBlockInfoTransaction) > 1)
            {
                res = BlockExecutionResult.TooManyTxsForParentChainBlock;
            }

            if (txs.Count == 0)
            {
                res = BlockExecutionResult.NoTransaction;
            }

            return new Tuple<BlockExecutionResult, List<Transaction>>(res, readyTxs);
        }

        /// <summary>
        /// Validate parent chain block info.
        /// </summary>
        /// <returns>
        /// Return false if validation failed and then that block execution would fail.
        /// </returns>
        private bool ValidateParentChainBlockInfoTransaction(ParentChainBlockInfo parentBlockInfo)
        {
            try
            {
                var cached = _clientManager.TryGetParentChainBlockInfo(parentBlockInfo);
                if (cached != null)
                    return cached.Equals(parentBlockInfo);
                //_logger.Warn("Not found cached parent block info");
                return false;
            }
            catch (Exception e)
            {
                if (e is ClientShutDownException)
                    return true;
                _logger.Warn("Parent chain block info validation failed.");
                return false;
            }
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
            var root = new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            var res = BlockExecutionResult.UpdateWorldStateSuccess;
            if (root != block.Header.MerkleTreeRootOfWorldState)
            {
                _logger?.Trace($"Block: {JsonSerializer.Instance.Serialize(block)}");
                _logger?.Trace($"results: {JsonSerializer.Instance.Serialize(results)}");
                _logger?.Trace($"{root.DumpHex()} != {block.Header.MerkleTreeRootOfWorldState.DumpHex()}");
                _logger?.Warn("ExecuteBlock - Incorrect merkle trees.");
                _logger?.Trace("Transaction Results:");
                foreach (var r in results)
                {
                    _logger?.Trace($"TransactionId: {r.TransactionId.DumpHex()}, " +
                                   $"StateHash: {r.StateHash.DumpHex()}，" +
                                   $"Status: {r.Status}, " +
                                   $"{r}");
                }
                
                throw new Exception("IncorrectStateMerkleTree");

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
            _logger?.Trace($"AppendingBlock {block.BlockHashToHex}");
            var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
            await blockchain.AddBlocksAsync(new List<IBlock> {block});
        }

        /// <summary>
        /// Update database
        /// </summary>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private void InsertTxs(List<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();

            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
        }

        /// <summary>
        /// Update cross chain block info, side chain block and parent block info if needed
        /// </summary>
        /// <param name="block"></param>
        /// <param name="txnRes"></param>
        /// <returns></returns>
        private async Task UpdateCrossChainInfo(IBlock block, List<TransactionResult> txnRes)
        {
            await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree,
                block.Header.ChainId, block.Header.Index);
            await _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                block.Body.BinaryMerkleTreeForSideChainTransactionRoots, block.Header.ChainId, block.Header.Index);

            // update side chain block info if execution succeed
            foreach (var blockInfo in block.Body.IndexedInfo)
            {
                /*if (!await _clientManager.TryUpdateAndRemoveSideChainBlockInfo(blockInfo))
                    // Todo: _clientManager would be chaos if this happened.
                    throw new InvalidCrossChainInfoException(
                        "Inconsistent side chain info. Something about side chain would be chaos if you see this. ", BlockExecutionResult.InvalidSideChainInfo);*/
                await _chainManagerBasic.UpdateCurrentBlockHeightAsync(blockInfo.ChainId, blockInfo.Height);
            }

            // update parent chain info
            /*if (block.ParentChainBlockInfo != null)
            {
                
                await _chainManagerBasic.UpdateCurrentBlockHeightAsync(block.ParentChainBlockInfo.ChainId,
                    block.ParentChainBlockInfo.Height);
            }*/
                
        }

        #endregion

        #region Rollback

        private async Task Rollback(IBlock block, IEnumerable<TransactionResult> txRes)
        {
            if (block == null)
                return;
            var blockChain = _chainService.GetBlockChain(block.Header.ChainId);
            await blockChain.RollbackStateForTransactions(
                txRes.Where(x => x.Status == Status.Mined).Select(x => x.TransactionId),
                block.Header.GetDisambiguationHash()
            );
        }

        #endregion

        /// <summary>
        /// Finish initial synchronization process.
        /// </summary>
        public void FinishInitialSync()
        {
            _clientManager.UpdateRequestInterval();
        }
    }
    
    internal class InvalidBlockException : Exception
    {
        public InvalidBlockException(string message) : base(message)
        {
        }
    }
}