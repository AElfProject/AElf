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
using AElf.Miner.Rpc.Exceptions;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;
using NServiceKit.Common.Extensions;
using ITxPoolService = AElf.ChainController.TxMemPool.ITxPoolService;
using AElf.Common;

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
            ClientManager clientManager, IBinaryMerkleTreeManager binaryMerkleTreeManager)
        {
            _txPoolService = txPoolService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _executingService = executingService;
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
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

            try
            {
                UpdataBPInfo(block);
                // get txn from pool
                var readyTxns = await CollectTransactions(block);
                var txnRes = await ExecuteTransactions(readyTxns, block);

                await UpdateWorldState(block);
                await AppendBlock(block);
                await InsertTxs(readyTxns, txnRes, block);
                await UpdateSideChainInfo(block);
                return true;
            }
            catch (Exception e)
            {
                if(e is InvalidBlockException)
                    await Interrupt(e.Message, block);
                else 
                    await Interrupt(e, block);
                return false;
            }
        }
        
        
        /// <summary>
        /// Execute transactions.
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<List<TransactionResult>> ExecuteTransactions(List<Transaction> readyTxs, IBlock block)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, block.Header.ChainId, Cts.Token);
            
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

        #region Before transaction execution

        /// <summary>
        /// Update BP info before execution
        /// </summary>
        /// <param name="block"></param>
        private void UpdataBPInfo(IBlock block)
        {
            var uncompressedPrivateKey = block.Header.P.ToByteArray();
            var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivateKey);
            var blockProducerAddress = recipientKeyPair.GetAddress();
            _stateDictator.ChainId = block.Header.ChainId;
            _stateDictator.BlockHeight = block.Header.Index - 1;
            _stateDictator.BlockProducerAccountAddress = blockProducerAddress;
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
            else if (!await ValidateSideChainBlockInfo(block))
            {
                // side chain info verification 
                // side chain info in this block cannot fit together with local side chain info.
                errlog = "Invalid side chain info";
                res = false;
            }

            if (!res)
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
            var txs = block.Body.Transactions;
            var readyTxs = new List<Transaction>();
            foreach (var id in txs)
            {
                if (!_txPoolService.TryGetTx(id, out var tx))
                {
                    tx = await _transactionManager.GetTransaction(id);
                    errlog = tx != null ? $"Transaction {id} already executed." : $"Cannot find transaction {id}";
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
        private async Task<bool> ValidateParentChainBlockInfoTransaction(Transaction transaction)
        {
            try
            {
                var parentBlockInfo = ParentChainBlockInfo.Parser.ParseFrom(transaction.Params.ToByteArray());
                var cached = await _clientManager.CollectParentChainBlockInfo();
                if(cached == null)
                    _logger.Trace("Not found cached parent block info");
                if(!cached.Equals(parentBlockInfo))
                    _logger.Trace($"Found cached parent block info at {cached.Height}, not {parentBlockInfo.Height}");
                return cached != null && cached.Equals(parentBlockInfo);
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
        private async Task UpdateWorldState(IBlock block)
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
            Transaction pcbTx = null;
            foreach (var t in executedTxs)
            {
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
                if (pcbTx == null || !pcbTx.GetHash().Equals(r.TransactionId)) 
                    return;
                var parentChainBlockInfo = ParentChainBlockInfo.Parser.ParseFrom(pcbTx.Params.ToByteArray());
                await _clientManager.UpdateParentChainBlockInfo(parentChainBlockInfo);
            });
        }
        
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
        }
        
        #endregion
        
        #region Rollback

        private async Task Interrupt(Exception e, IBlock block = null)
        {
            _logger.Error(e);
            await Rollback(block);
        }
        
        private async Task Interrupt(string log, IBlock block = null)
        {
            _logger.Warn(log);
            await Rollback(block);
        }

        private async Task Rollback(IBlock block)
        {
            if(block == null)
                return;
            var blockChain = _chainService.GetBlockChain(block.Header.ChainId);
            var txToRevert = await blockChain.RollbackToHeight(block.Header.Index - 1);
            await _txPoolService.Revert(txToRevert);
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