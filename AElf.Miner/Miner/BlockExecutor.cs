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
        private ILogger _logger;
        private ClientManager _clientManager;

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
            var readyTxs = new List<Transaction>();
            try
            {
                if (Cts == null || Cts.IsCancellationRequested)
                {
                    _logger?.Trace("ExecuteBlock - Execution cancelled.");
                    return false;
                }
                var map = new Dictionary<Hash, HashSet<ulong>>();

                if (block?.Header == null || block.Body?.Transactions == null || block.Body.Transactions.Count <= 0)
                    _logger?.Trace("ExecuteBlock - Null block or no transactions.");

                _logger?.Trace($"Executing block {block.GetHash()}");
                
                var uncompressedPrivKey = block.Header.P.ToByteArray();
                var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
                var blockProducerAddress = recipientKeyPair.GetAddress();

                // side chain info verification 
                if (!ValidateSideChainBlockInfo(block))
                {
                    // side chain info in block cannot fit together with local side chain info.
                    _logger?.Debug("Wrong side chain info");
                    return false;
                }
                
                _stateDictator.ChainId = block.Header.ChainId;
                _stateDictator.BlockHeight = block.Header.Index - 1;
                _stateDictator.BlockProducerAccountAddress = blockProducerAddress;
                
                var txs = block.Body?.Transactions;
                if (txs != null)
                    foreach (var id in txs)
                    {
                        if (!_txPoolService.TryGetTx(id, out var tx))
                        {
                            tx = await _transactionManager.GetTransaction(id);
                            if (tx != null)
                            {
                                var txRes = await _transactionResultManager.GetTransactionResultAsync(id);
                                _logger?.Debug($"Transaction {id} already executed.\n{txRes}");
                            }
                            else
                            {
                                throw new Exception($"Cannot find transaction {id}");    
                            }
                        }

                        readyTxs.Add(tx);
                    }

                
                var traces = readyTxs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(readyTxs, block.Header.ChainId, Cts.Token);
                
                foreach (var trace in traces)
                {
                    _logger?.Trace($"Trace {trace.TransactionId.ToHex()}, {trace.StdErr}");
                }
                
                var results = new List<TransactionResult>();
                foreach (var trace in traces)
                {
                    var res = new TransactionResult()
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

                var addrs = await InsertTxs(readyTxs, results, block);
                await _txPoolService.UpdateAccountContext(addrs);

                await _stateDictator.SetBlockHashAsync(block?.GetHash());
                await _stateDictator.SetStateHashAsync(block?.GetHash());
                
                await _stateDictator.SetWorldStateAsync();
                var ws = await _stateDictator.GetLatestWorldStateAsync();

                if (ws == null)
                {
                    _logger?.Debug($"ExecuteBlock - Could not get world state.");
                    await Rollback(readyTxs);
                    return false;
                }

                if (await ws.GetWorldStateMerkleTreeRootAsync() != block?.Header.MerkleTreeRootOfWorldState)
                {
                    _logger?.Debug($"ExecuteBlock - Incorrect merkle trees.");
                    _logger?.Debug($"Merkle tree root hash of execution: {(await ws.GetWorldStateMerkleTreeRootAsync()).ToHex()}");
                    _logger?.Debug($"Merkle tree root hash of received block: {block?.Header.MerkleTreeRootOfWorldState.ToHex()}");

                    await Rollback(readyTxs);
                    return false;
                }

                var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
                await blockchain.AddBlocksAsync(new List<IBlock> {block});
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"ExecuteBlock - Execution failed.");
                await Rollback(readyTxs);
                return false;
            }

            return true;
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
        private async Task<HashSet<Hash>> InsertTxs(List<Transaction> executedTxs, List<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            var addrs = new HashSet<Hash>();
            foreach (var t in executedTxs)
            {
                addrs.Add(t.From);
                await _transactionManager.AddTransactionAsync(t);
                _txPoolService.RemoveAsync(t.GetHash());
            }
            
            txResults.ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
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
            await _txPoolService.Revert(readyTxs);
            await _stateDictator.RollbackToPreviousBlock();
        }
        
        public void Start()
        {
            Cts = new CancellationTokenSource();
        }
    }
}