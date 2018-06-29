using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using Akka.Configuration;

namespace AElf.Kernel.Miner
{
    public class BlockExecutor : IBlockExecutor
    {
        private readonly ITxPoolService _txPoolService;
        private IParallelTransactionExecutingService _parallelTransactionExecutingService;
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly IWorldStateDictator _worldStateDictator;

        public BlockExecutor(ITxPoolService txPoolService, IChainManager chainManager, 
            IBlockManager blockManager, IWorldStateDictator worldStateDictator)
        {
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
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
                    return false;
                
                var map = new Dictionary<Hash, HashSet<ulong>>();
                var txs = block.Body.Transactions;
                foreach (var id in txs)
                {
                    if (!_txPoolService.TryGetTx(id, out var tx))
                        return false;
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
                                return false;
                        }
                    }
                    
                    // get ready txs from pool
                    var txList = await _txPoolService.GetReadyTxsAsync(addr, ids.Min(), (ulong)ids.Count);
                    if (txList == null)
                        return false;
                    
                    ready.AddRange(txList);
                }
                
                var traces = ready.Count == 0
                    ? new List<TransactionTrace>()
                    : await _parallelTransactionExecutingService.ExecuteAsync(ready, block.Header.ChainId);
                
                // TODO: commit tx results
        
                await _worldStateDictator.SetWorldStateAsync(block.Header.PreviousBlockHash);
                var ws = await _worldStateDictator.GetWorldStateAsync(block.Header.PreviousBlockHash);

                if (ws == null || await ws.GetWorldStateMerkleTreeRootAsync() !=
                    block.Header.MerkleTreeRootOfWorldState)
                    return false;
                
                await _chainManager.AppendBlockToChainAsync(block);
                await _blockManager.AddBlockAsync(block);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            
            return true;
        }

        public void Start(IParallelTransactionExecutingService parallelTransactionExecutingService)
        {
            Cts = new CancellationTokenSource();
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
        }
    }
}