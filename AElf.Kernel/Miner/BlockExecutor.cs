using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using NLog;

namespace AElf.Kernel.Miner
{
    [LoggerName(nameof(BlockExecutor))]
    public class BlockExecutor : IBlockExecutor
    {
        private readonly ITxPoolService _txPoolService;
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly IConcurrencyExecutingService _concurrencyExecutingService;
        private IGrouper _grouper;
        private ILogger _logger;

        public BlockExecutor(ITxPoolService txPoolService, IChainManager chainManager,
            IBlockManager blockManager, IWorldStateDictator worldStateDictator,
            IConcurrencyExecutingService concurrencyExecutingService, ILogger logger)
        {
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
            _concurrencyExecutingService = concurrencyExecutingService;
            _logger = logger;
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }

        /// <inheritdoc/>
        public async Task<bool> ExecuteBlock(IBlock block)
        {
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

                var uncompressedPrivKey = block.Header.P.ToByteArray();
                var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
                _worldStateDictator.BlockProducerAccountAddress = recipientKeyPair.GetAddress();
                
                var txs = block.Body.Transactions;
                foreach (var id in txs)
                {
                    if (!_txPoolService.TryGetTx(id, out var tx))
                    {
                        _logger?.Trace($"ExecuteBlock - Transaction not in pool {id.Value.ToByteArray().ToHex()}.");
                        return false;
                    }

                    var from = tx.From;

                    if (!map.ContainsKey(from))
                        map[from] = new HashSet<ulong>();

                    map[from].Add(tx.IncrementId);
                }

                // promote txs from these address
                await _txPoolService.PromoteAsync(map.Keys.ToList());

                var ready = new List<ITransaction>();
                foreach (var fromTxs in map)
                {
                    var addr = fromTxs.Key;
                    var ids = fromTxs.Value;

                    // return false if not continuousa
                    if (ids.Count != 1)
                    {
                        foreach (var id in ids)
                        {
                            if (!ids.Contains(id - 1) && !ids.Contains(id + 1))
                            {
                                _logger?.Trace($"ExecuteBlock - Non continuous ids, id {id}.");
                                return false;
                            }
                        }
                    }

                    // get ready txs from pool
                    var txList = await _txPoolService.GetReadyTxsAsync(addr, ids.Min(), (ulong) ids.Count);

                    if (txList == null)
                    {
                        _logger?.Trace($"ExecuteBlock - No transactions are ready.");
                        return false;
                    }

                    ready.AddRange(txList);
                }

                var traces = ready.Count == 0
                    ? new List<TransactionTrace>()
                    : await _concurrencyExecutingService.ExecuteAsync(ready, block.Header.ChainId, _grouper);

                foreach (var trace in traces)
                {
                    _logger?.Trace($"Trace {trace.TransactionId}, {trace.StdErr}");
                }

                await _worldStateDictator.SetWorldStateAsync(block.Header.PreviousBlockHash);
                var ws = await _worldStateDictator.GetWorldStateAsync(block.Header.PreviousBlockHash);

                if (ws == null)
                {
                    _logger?.Trace($"ExecuteBlock - Could not get world state.");
                    return false;
                }

                if (await ws.GetWorldStateMerkleTreeRootAsync() != block.Header.MerkleTreeRootOfWorldState)
                {
                    _logger?.Trace($"ExecuteBlock - Incorrect merkle trees.");
                    return false;
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

                await _txPoolService.ResetAndUpdate(results);
                await _chainManager.AppendBlockToChainAsync(block);
                await _blockManager.AddBlockAsync(block);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"ExecuteBlock - Execution failed.");
                return false;
            }

            return true;
        }

        public void Start(IGrouper grouper)
        {
            Cts = new CancellationTokenSource();
            _grouper = grouper;
        }
    }
}