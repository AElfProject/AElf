using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.Miner
{
    public class Miner : IMiner
    {
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ITxPoolService _txPoolService;
        private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService;
        private readonly IWorldStateManager _worldStateManager;
        
        private readonly Dictionary<ulong, IBlock> waiting = new Dictionary<ulong, IBlock>();

        private MinerLock Lock { get; } = new MinerLock();
        
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; } 
        
        
        /// <summary>
        /// event set to mine
        /// </summary>
        public AutoResetEvent MiningResetEvent { get; private set; }
        
        public IMinerConfig Config { get; }

        public Hash Coinbase => Config.CoinBase;

        public Miner(IBlockGenerationService blockGenerationService, IMinerConfig config, 
            ITxPoolService txPoolService, IParallelTransactionExecutingService parallelTransactionExecutingService,
            IWorldStateManager worldStateManager)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _txPoolService = txPoolService;
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
            _worldStateManager = worldStateManager;
        }

        
        public async Task<IBlock> Mine()
        {
            if (Cts == null || Cts.IsCancellationRequested)
                return null;
            
            var ready = await _txPoolService.GetReadyTxsAsync(Config.TxCount);
            // TODO：dispatch txs with ISParallel, return list of tx results
            
            var traces =  await _parallelTransactionExecutingService.ExecuteAsync(ready, Config.ChainId);
            
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
                    /* Not ready to support delayed commit yet #185
                    foreach (var vc in trace.ValueChanges)
                    {
                        await _worldStateManager.ApplyStateValueChangeAsync(vc, Config.ChainId);
                    }
                    */
                    res.Status = Status.Mined;
                }
                else
                {
                    res.Status = Status.Failed;
                }
                results.Add(res);
            }
            
            // generate block
            var block = await _blockGenerationService.GenerateBlockAsync(Config.ChainId, results);
            
            // reset Promotable and update account context
            await _txPoolService.ResetAndUpdate(results);

            return block;
        }

        
        /// <summary>
        /// start mining  
        /// </summary>
        public void Start()
        {
            Cts = new CancellationTokenSource();
            //MiningResetEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// stop mining
        /// </summary>
        public void Stop()
        {
            Lock.WriteLock(() =>
            {
                Cts.Cancel();
                Cts.Dispose();
                //MiningResetEvent.Dispose();
            });
            
        }

    }
    
    /// <inheritdoc />
    /// <summary>
    /// A lock for managing asynchronous access to memory pool.
    /// </summary>
    public class MinerLock : ReaderWriterLock
    {
    }
}