using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using AElf.Types.CSharp;
using Google.Protobuf;
using NLog;
using NServiceKit.Common.Extensions;
using AElf.Miner.TxMemPool;
using Easy.MessageHub;

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

        public BlockExecutor(IChainService chainService, IExecutingService executingService,
            ITransactionResultManager transactionResultManager, ClientManager clientManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ITxHub txHub)
        {
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _txHub = txHub;
            _logger = LogManager.GetLogger(nameof(BlockExecutor));
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }

        private string _current;

        /// <inheritdoc/>
        public async Task<BlockExecutionResult> ExecuteBlock(IBlock block)
        {
            if (_current != null)
            {
                _logger?.Trace($"Prevent block {block.BlockHashToHex} from re-entering block execution, " +
                               $"for block {_current} is being executing.");
                return BlockExecutionResult.AlreadyReceived;
            }

            _current = block.BlockHashToHex;

            var result = await Prepare(block);
            if (result.IsFailed())
            {
                _current = null;
                return result;
            }

            _logger?.Trace($"Executing block {block.GetHash()}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            MessageHub.Instance.Publish(new ExecutionStateChanged(true));

            var txnRes = new List<TransactionResult>();
            var readyTxs = new List<Transaction>();
            try
            {
                // get txn from pool
                var tuple = await CollectTransactions(block);
                result = tuple.Item1;
                if (result.IsFailed())
                {
                    return result;
                }

                readyTxs = tuple.Item2;

                var trs = await _txHub.GetReceiptsForAsync(readyTxs);
                foreach (var tr in trs)
                {
                    if (!tr.IsExecutable)
                    {
                        throw new InvalidBlockException($"Transaction is not executable. {tr}");
                    }
                }

                txnRes = await ExecuteTransactions(readyTxs, block.Header.ChainId, block.Header.GetDisambiguationHash());
                txnRes = SortToOriginalOrder(txnRes, readyTxs);

                var blockChain = _chainService.GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId));
                if (await blockChain.GetBlockByHashAsync(block.GetHash()) != null)
                {
                    return BlockExecutionResult.AlreadyReceived;
                }

                result = await UpdateWorldState(block, txnRes);
                if (result.IsFailed())
                {
                    return result;
                }
                
                await UpdateSideChainInfo(block);

                /*** Need-to-rollback boundary ***/

                await AppendBlock(block);
                await InsertTxs(readyTxs, txnRes, block);

                await _txHub.OnNewBlock((Block)block);

                return BlockExecutionResult.Success;
            }
            catch (Exception e)
            {
                var res = BlockExecutionResult.Failed;
                if (e is InvalidCrossChainInfoException i)
                {
                    _logger?.Warn(e, $"Exception while execute block {block.BlockHashToHex}.");
                    res = i.Result;
                }
                else
                {
                    _logger?.Error(e, "Exception while execute block {block.BlockHashToHex}.");
                }

                // TODO, no wait may need improve
                Rollback(block, txnRes).ConfigureAwait(false);

                return res;
            }
            finally
            {
                MessageHub.Instance.Publish(new ExecutionStateChanged(false));
                _current = null;
                stopwatch.Stop();
                _logger?.Info($"Execute block {block.BlockHashToHex} with txs {readyTxs.Count}, " +
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
            Hash disambiguationHash)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, chainId, Cts.Token, disambiguationHash);

            var results = new List<TransactionResult>();
            foreach (var trace in traces)
            {
                var res = new TransactionResult
                {
                    TransactionId = trace.TransactionId,
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
        private async Task<BlockExecutionResult> Prepare(IBlock block)
        {
            string errorLog = null;
            var res = BlockExecutionResult.PrepareSuccess;
            if (Cts == null || Cts.IsCancellationRequested)
            {
                errorLog = "ExecuteBlock - Execution cancelled.";
                res = BlockExecutionResult.ExecutionCancelled;
            }
            else if (block?.Header == null)
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
            return block.Body.IndexedInfo.All(_clientManager.CheckSideChainBlockInfo);
        }

        /// <summary>
        /// Get txs from tx pool
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCrossChainInfoException"></exception>
        private async Task<Tuple<BlockExecutionResult, List<Transaction>>> CollectTransactions(IBlock block)
        {
            //string errorLog = null;
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
                    if (!await ValidateParentChainBlockInfoTransaction(parentBlockInfo))
                    {
                        //errorLog = "Invalid parent chain block info.";
                        res = BlockExecutionResult.InvalidParentChainBlockInfo;
                        break;
                    }

                    // for update
                    block.ParentChainBlockInfo = parentBlockInfo;
                }

                readyTxs.Add(tx);
            }

            if (res.IsSuccess() && readyTxs.Count(t => t.Type == TransactionType.CrossChainBlockInfoTransaction) > 1)
            {
                res = BlockExecutionResult.TooManyTxsForParentChainBlock;
                //errorLog = "More than one transaction to record parent chain block info.";
            }

            return new Tuple<BlockExecutionResult, List<Transaction>>(res, readyTxs);
        }

        /// <summary>
        /// Validate parent chain block info.
        /// </summary>
        /// <returns>
        /// Return false if validation failed and then that block execution would fail.
        /// </returns>
        private async Task<bool> ValidateParentChainBlockInfoTransaction(ParentChainBlockInfo parentBlockInfo)
        {
            try
            {
                var cached = await _clientManager.TryGetParentChainBlockInfo();
                if (cached == null)
                {
                    _logger.Warn("Not found cached parent block info");
                    return false;
                }

                if (cached.Equals(parentBlockInfo))
                    return true;

                _logger.Trace($"Cached parent block info is {cached}");
                _logger.Trace($"Parent block info in transaction is {parentBlockInfo}");
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
        private async Task<BlockExecutionResult> UpdateWorldState(IBlock block, IEnumerable<TransactionResult> results)
        {
            var root = new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            var res = BlockExecutionResult.UpdateWorldStateSuccess;
            if (root != block.Header.MerkleTreeRootOfWorldState)
            {
                _logger?.Warn("ExecuteBlock - Incorrect merkle trees.");
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
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private async Task InsertTxs(List<Transaction> executedTxs, List<TransactionResult> txResults, IBlock block)
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
        /// <returns></returns>
        /// <exception cref="InvalidCrossChainInfoException"></exception>
        private async Task UpdateSideChainInfo(IBlock block)
        {
            await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree,
                block.Header.ChainId, block.Header.Index);
            await _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                block.Body.BinaryMerkleTreeForSideChainTransactionRoots, block.Header.ChainId, block.Header.Index);

            // update side chain block info if execution succeed
            foreach (var blockInfo in block.Body.IndexedInfo)
            {
                if (!await _clientManager.TryUpdateAndRemoveSideChainBlockInfo(blockInfo))
                    // Todo: _clientManager would be chaos if this happened.
                    throw new InvalidCrossChainInfoException(
                        "Inconsistent side chain info. Something about side chain would be chaos if you see this. ",
                        BlockExecutionResult.InvalidSideChainInfo);
            }

            // update parent chain info
            if (!await _clientManager.UpdateParentChainBlockInfo(block.ParentChainBlockInfo))
                throw new InvalidCrossChainInfoException(
                    "Inconsistent parent chain info. Something about parent chain would be chaos if you see this. ",
                    BlockExecutionResult.InvalidSideChainInfo);
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

        public void Init()
        {
            Cts = new CancellationTokenSource();
        }
    }

    internal class InvalidCrossChainInfoException : Exception
    {
        public BlockExecutionResult Result { get; }

        public InvalidCrossChainInfoException(string message, BlockExecutionResult result) : base(message)
        {
            Result = result;
        }
    }

    internal class InvalidBlockException : Exception
    {
        public InvalidBlockException(string message) : base(message)
        {
        }
    }
}