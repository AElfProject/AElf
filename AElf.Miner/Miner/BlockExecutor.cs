using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Execution;
using AElf.Execution.Scheduling;
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

        public BlockExecutor(ITxPoolService txPoolService, IChainService chainService,
            IStateDictator stateDictator,
            IExecutingService executingService, 
            ILogger logger, ITransactionManager transactionManager, ITransactionResultManager transactionResultManager)
        {
            _txPoolService = txPoolService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _executingService = executingService;
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }

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

                if (block?.Body?.Transactions == null || block.Body.Transactions.Count <= 0)
                    _logger?.Trace($"ExecuteBlock - Null block or no transactions.");

                var uncompressedPrivKey = block?.Header.P.ToByteArray();
                var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
                var blockProducerAddress = recipientKeyPair.GetAddress();
                
                _stateDictator.ChainId = block?.Header.ChainId;
                _stateDictator.BlockHeight = block?.Header.Index - 1 ?? 0;
                _stateDictator.BlockProducerAccountAddress = blockProducerAddress;
                
                var txs = block?.Body?.Transactions;
                if (txs != null)
                    foreach (var id in txs)
                    {
                        if (!_txPoolService.TryGetTx(id, out var tx))
                        {
                            throw new Exception($"Cannot find transaction {id}");
                            return false;
                        }

                        readyTxs.Add(tx);
                    }

//                foreach (var id in txs)
//                {
//                    if (!_txPoolService.TryGetTx(id, out var tx))
//                    {
//                        _logger?.Trace($"ExecuteBlock - Transaction not in pool {id.ToHex()}.");
//                        await Rollback(readyTxs);
//                        return false;
//                    }
//                    readyTxs.Add(tx);
//                    
//                    // remove from tx collection
//                    _txPoolService.RemoveAsync(tx.GetHash());
//                    var from = tx.From;
//                    if (!map.ContainsKey(from))
//                        map[from] = new HashSet<ulong>();
//
//                    map[from].Add(tx.IncrementId);
//                }
//
//                // promote txs from these address
//                //await _txPoolService.PromoteAsync(map.Keys.ToList());
//                foreach (var fromTxs in map)
//                {
//                    var addr = fromTxs.Key;
//                    var ids = fromTxs.Value; 
//
//                    // return false if not continuousa
//                    if (ids.Count != 1)
//                    {
//                        foreach (var id in ids)
//                        {
//                            if (!ids.Contains(id - 1) && !ids.Contains(id + 1))
//                            {
//                                _logger?.Trace($"ExecuteBlock - Non continuous ids, id {id}.");
//                                await Rollback(readyTxs);
//                                return false;
//                            }
//                        }
//                    }
//
//                    // get ready txs from pool
//                    var ready = await _txPoolService.GetReadyTxsAsync(addr, ids.Min(), (ulong) ids.Count);
//
//                    if (ready) continue;
//                    _logger?.Trace($"ExecuteBlock - No transactions are ready.");
//                    await Rollback(readyTxs);
//                    return false;
//                }
                
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
                    _logger?.Trace($"ExecuteBlock - Could not get world state.");
                    await Rollback(readyTxs);
                    return false;
                }

                if (await ws.GetWorldStateMerkleTreeRootAsync() != block?.Header.MerkleTreeRootOfWorldState)
                {
                    _logger?.Trace($"ExecuteBlock - Incorrect merkle trees.");
                    _logger?.Trace($"Merkle tree root hash of execution: {(await ws.GetWorldStateMerkleTreeRootAsync()).ToHex()}");
                    _logger?.Trace($"Merkle tree root hash of received block: {block?.Header.MerkleTreeRootOfWorldState.ToHex()}");

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
        /// update database
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
        /// withdraw txs in tx pool
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <returns></returns>
        private async Task Rollback(List<Transaction> readyTxs)
        {
            await _txPoolService.RollBack(readyTxs);
            await _stateDictator.RollbackToPreviousBlock();
        }
        
        public void Start()
        {
            Cts = new CancellationTokenSource();
        }
    }
}