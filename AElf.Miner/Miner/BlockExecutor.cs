using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;
using NServiceKit.Common.Extensions;
using ITxPoolService = AElf.ChainController.TxMemPool.ITxPoolService;

namespace AElf.Miner.Miner
{
    [LoggerName(nameof(BlockExecutor))]
    public class BlockExecutor : IBlockExecutor
    {
        private readonly ITxPoolService _txPoolService;
        private readonly IChainService _chainService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IStateDictator _stateDictator;
        private readonly IExecutingService _executingService;
        private readonly ILogger _logger;
        private readonly ClientManager _clientManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;

        public BlockExecutor(ITxPoolService txPoolService, IChainService chainService,
            IStateDictator stateDictator, IExecutingService executingService, 
            ILogger logger, ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, 
            ClientManager clientManager)
        {
            _txPoolService = txPoolService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _executingService = executingService;
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _clientManager = clientManager;
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }

        /// <inheritdoc/>
        public async Task<bool> ExecuteBlock(IBlock block)
        {
            if (!await Prepare(block))
            {
                return false;
            }
            _logger?.Trace($"Executing block {block.GetHash()}");

            var uncompressedPrivKey = block.Header.P.ToByteArray();
            var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            var blockProducerAddress = recipientKeyPair.GetAddress();
            _stateDictator.ChainId = block.Header.ChainId;
            _stateDictator.BlockHeight = block.Header.Index - 1;
            _stateDictator.BlockProducerAccountAddress = blockProducerAddress;
            var readyTxs = new List<Transaction>();
            try
            {
                readyTxs= await CollectTransactions(block);
                if (readyTxs == null)
                    return false;
            
                var results = await ExecuteTransactions(readyTxs, block.Header.ChainId);
                if (results == null)
                {
                    return false;
                }
                await InsertTxs(readyTxs, results, block);
                bool res = await UpdateState(block);
                if (!res)
                {
                    await Rollback(readyTxs);
                    return false;
                }
                var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
                await blockchain.AddBlocksAsync(new List<IBlock> {block});
                await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree, block.Header.ChainId,
                    block.Header.Index);
                await _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                    block.Body.BinaryMerkleTreeForSideChainTransactionRoots, block.Header.ChainId, block.Header.Index);
            }
            catch (Exception e)
            {
                string errlog = $"ExecuteBlock - Execution failed with exception {e}";
                await Interrupt(errlog, readyTxs, e);
                return false;
            }

            return true;
        }

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
            else if (!ValidateSideChainBlockInfo(block))
            {
                // side chain info verification 
                // side chain info in block cannot fit together with local side chain info.
                errlog = "Invalid side chain info";
                res = false;
            }

            if (!res)
                await Interrupt(errlog);
            return res;
        }
        
        /// <summary>
        /// Execute transactions.
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="chainId"></param>
        /// <returns></returns>
        private async Task<List<TransactionResult>> ExecuteTransactions(List<Transaction> readyTxs, Hash chainId)
        {
            try
            {
                var traces = readyTxs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(readyTxs, chainId, Cts.Token);
                
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
                    }
                    else
                    {
                        res.Status = Status.Failed;
                        res.RetVal = ByteString.CopyFromUtf8(trace.StdErr);
                    }
                    results.Add(res);
                }

                return results;
            }
            catch (Exception e)
            {
                await Interrupt(e.ToString(), readyTxs, e);
                return null;
            }
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
            var txs = block.Body.Transactions;
            var readyTxs = new List<Transaction>();
            foreach (var id in txs)
            {
                if (!_txPoolService.TryGetTx(id, out var tx))
                {
                    tx = await _transactionManager.GetTransaction(id);
                    errlog = tx != null ? "Transaction {id} already executed." : $"Cannot find transaction {id}";
                    res = false;
                    break;
                }

                if (tx.Type == TransactionType.CrossChainBlockInfoTransaction 
                    && !await ValidateParentChainBlockInfoTransaction(tx))
                {
                    errlog = "Invalid parent chain block info.";
                    res = false;
                    break;
                }
                readyTxs.Add(tx);
            }

            if (res && readyTxs.Count(t => t.Type == TransactionType.CrossChainBlockInfoTransaction) > 1)
            {
                res = false;
                errlog = "More than one transaction to record parent chain block info.";
            }
            
            if (!res)
                await Interrupt(errlog, readyTxs);
            return res ? readyTxs : null;
        }

        /// <summary>
        /// Update system state.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<bool> UpdateState(IBlock block)
        {
            await _stateDictator.SetBlockHashAsync(block.GetHash());
            await _stateDictator.SetStateHashAsync(block.GetHash());
            await _stateDictator.SetWorldStateAsync();
            var ws = await _stateDictator.GetLatestWorldStateAsync();
            string errlog = null;
            bool res = true;
            if (ws == null)
            {
                errlog = "ExecuteBlock - Could not get world state.";
                res = false;
            }
            else if (await ws.GetWorldStateMerkleTreeRootAsync() != block.Header.MerkleTreeRootOfWorldState)
            {
                errlog = "ExecuteBlock - Incorrect merkle trees.";
                res = false;
            }
            if(!res)
                await Interrupt(errlog);
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
            var sideChainBlockIndexedInfo = block.Body.IndexedInfo.Aggregate(
                new Dictionary<Hash, SortedList<ulong, SideChainBlockInfo>>(),
                (m, cur) =>
                {
                    if (!m.TryGetValue(cur.ChainId, out var sortedList))
                    {
                        sortedList = m[cur.ChainId] = new SortedList<ulong, SideChainBlockInfo>();
                    }
                    sortedList.Add(cur.Height, cur);
                    return m;
                });
            foreach (var _ in sideChainBlockIndexedInfo)
            {
                foreach (var blockInfo in _.Value)
                {
                    if (_clientManager.TryRemoveSideChainBlockInfo(blockInfo.Value))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private async Task<HashSet<Hash>> InsertTxs(List<Transaction> executedTxs, List<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            var addrs = new HashSet<Hash>();
            Transaction pcbTx = null;
            foreach (var t in executedTxs)
            {
                addrs.Add(t.From);
                await _transactionManager.AddTransactionAsync(t);
                _txPoolService.RemoveAsync(t.GetHash());
                
                // this could be improved
                if (t.Type == TransactionType.CrossChainBlockInfoTransaction)
                    pcbTx = t;
            }
            
            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
                if (pcbTx != null && pcbTx.GetHash().Equals(r.TransactionId))
                {
                    var parentChainBlockInfo = ParentChainBlockInfo.Parser.ParseFrom(pcbTx.Params.ToByteArray());
                    await _clientManager.UpdateParentChainBlockInfo(parentChainBlockInfo);
                }
            });
            return addrs;
        }

        /// <summary>
        /// Withdraw txs in tx pool
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <returns></returns>
        private async Task Rollback(List<Transaction> readyTxs)
        {
            await _stateDictator.RollbackToPreviousBlock();
            if (readyTxs == null)
                return;
            await _txPoolService.Revert(readyTxs);
        }

        private async Task Interrupt(string log, List<Transaction> readyTxs = null, Exception e = null)
        {
            if(e == null)
                _logger.Debug(log);
            else 
                _logger.Error(e, log);
            await Rollback(readyTxs);
        }
        
        /// <summary>
        /// Validate parent chain block info.
        /// </summary>
        /// <param name="transaction">System transaction with parent chain block info.</param>
        /// <returns>
        /// Return false if validation failed and then that block execution would fail.
        /// </returns>
        private async Task<bool> ValidateParentChainBlockInfoTransaction(Transaction transaction)
        {
            try
            {
                var parentBlockInfo = ParentChainBlockInfo.Parser.ParseFrom(transaction.Params.ToByteArray());
                return (await _clientManager.CollectParentChainBlockInfo()).Equals(parentBlockInfo);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Parent chain block info validation failed.");
                return false;
            }
        }
        
        public void Start()
        {
            Cts = new CancellationTokenSource();
        }
    }
}