using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using AElf.Types.CSharp;
using Google.Protobuf;
using NLog;
using NServiceKit.Common.Extensions;

namespace AElf.Synchronization.BlockExecution
{
    public class BlockExecutor : IBlockExecutor
    {
        private readonly IChainService _chainService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IExecutingService _executingService;
        private readonly ILogger _logger;
        private readonly ClientManager _clientManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;

        public BlockExecutor(IChainService chainService, IExecutingService executingService,
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager,
            ClientManager clientManager, IBinaryMerkleTreeManager binaryMerkleTreeManager)
        {
            _chainService = chainService;
            _executingService = executingService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;

            _logger = LogManager.GetLogger(nameof(BlockExecutor));
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }

        /// <inheritdoc/>
        public async Task<BlockExecutionResult> ExecuteBlock(IBlock block)
        {
            if (!await Prepare(block))
            {
                return BlockExecutionResult.Failed;
            }

            var txnRes = new List<TransactionResult>();
            try
            {
                // get txn from pool
                var readyTxns = await CollectTransactions(block);
                txnRes = await ExecuteTransactions(readyTxns, block, block.Header.GetDisambiguationHash());

                await UpdateWorldState(block, txnRes);
                await UpdateSideChainInfo(block);
                await AppendBlock(block);
                await InsertTxs(readyTxns, txnRes, block);

                _logger?.Info($"Execute block {block.BlockHashToHex}.");

                return BlockExecutionResult.Success;
            }
            catch (Exception e)
            {
                if (e is InvalidBlockException)
                {
                    _logger?.Warn(e, "Exception while execute block.");
                }
                else
                {
                    _logger?.Error(e, "Exception while execute block.");
                }

                // TODO, no wait may need improve
                Rollback(block, txnRes).ConfigureAwait(false);

                return BlockExecutionResult.Failed;
            }
        }


        /// <summary>
        /// Execute transactions.
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<List<TransactionResult>> ExecuteTransactions(List<Transaction> readyTxs, IBlock block, Hash disambiguationHash)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, block.Header.ChainId, Cts.Token, disambiguationHash);

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

        #region Before transaction execution

        /// <summary>
        /// Verify block components and validate side chain info if needed
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<bool> Prepare(IBlock block)
        {
            string errlog = null;
            bool res = true;
            if (Cts == null || Cts.IsCancellationRequested)
            {
                errlog = "ExecuteBlock - Execution cancelled.";
                res = false;
            }
            else if (block?.Header == null || block.Body?.Transactions == null || block.Body.Transactions.Count <= 0)
            {
                errlog = "ExecuteBlock - Null block or no transactions.";
                res = false;
            }
            else if (!await ValidateSideChainBlockInfo(block))
            {
                // side chain info verification 
                // side chain info in this block cannot fit together with local side chain info.
                errlog = "Invalid side chain info";
                res = false;
            }

            if (!res)
                _logger?.Warn(errlog);

            return res;
        }

        /// <summary>
        /// Check side chain info.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>
        /// Return true if side chain info is consistent with local node, else return false;
        /// </returns>
        private async Task<bool> ValidateSideChainBlockInfo(IBlock block)
        {
            return block.Body.IndexedInfo.All(_clientManager.CheckSideChainBlockInfo);
        }

        /// <summary>
        /// Get txs from tx pool
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<List<Transaction>> CollectTransactions(IBlock block)
        {
            string errlog = null;
            bool res = true;
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
                        errlog = "Invalid parent chain block info.";
                        res = false;
                        break;
                    }

                    // for update
                    block.ParentChainBlockInfo = parentBlockInfo;
                }

                readyTxs.Add(tx);
            }

            if (res && readyTxs.Count(t => t.Type == TransactionType.CrossChainBlockInfoTransaction) > 1)
            {
                res = false;
                errlog = "More than one transaction to record parent chain block info.";
            }

            if (!res)
                throw new InvalidBlockException(errlog);

            return readyTxs;
        }

        /// <summary>
        /// Validate parent chain block info.
        /// </summary>
        /// <param name="transaction">System transaction with parent chain block info.</param>
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

                _logger.Warn($"Cached parent block info is {cached}");
                _logger.Warn($"Parent block info in transaction is {parentBlockInfo}");
                return false;
            }
            catch (Exception e)
            {
                if (e is ClientShutDownException)
                    return true;
                _logger.Error(e, "Parent chain block info validation failed.");
                return false;
            }
        }

        #endregion

        #region After transaction execution

        /// <summary>
        /// Update system state.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task UpdateWorldState(IBlock block, IEnumerable<TransactionResult> results)
        {
            var root = new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            string errlog = null;
            bool res = true;
            if (root != block.Header.MerkleTreeRootOfWorldState)
            {
                errlog = "ExecuteBlock - Incorrect merkle trees.";
                res = false;
            }

            if (!res)
                throw new InvalidBlockException(errlog);
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
            foreach (var t in executedTxs)
            {
                await _transactionManager.AddTransactionAsync(t);
            }

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
        /// <exception cref="InvalidBlockException"></exception>
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
                    throw new InvalidBlockException(
                        "Inconsistent side chain info. Something about side chain would be chaos if you see this. ");
            }

            // update parent chain info
            if (!await _clientManager.UpdateParentChainBlockInfo(block.ParentChainBlockInfo))
                throw new InvalidBlockException(
                    "Inconsistent parent chain info. Something about parent chain would be chaos if you see this. ");
        }

        #endregion

        #region Rollback

        private async Task Rollback(IBlock block, IEnumerable<TransactionResult> txRes)
        {
            if (block == null)
                return;
            var blockChain = _chainService.GetBlockChain(block.Header.ChainId);
            await blockChain.RollbackStateForTransactions(
                txRes.Where(x => x.Status == Status.Mined).Select(x => x.TransactionId), block.Header.GetDisambiguationHash()
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

    internal class InvalidBlockException : Exception
    {
        public InvalidBlockException(string message) : base(message)
        {
        }
    }
}