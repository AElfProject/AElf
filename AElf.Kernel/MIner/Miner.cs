using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using ReaderWriterLock = AElf.Kernel.Lock.ReaderWriterLock;


namespace AElf.Kernel.MIner
{
    public class Miner : IMiner
    {
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ITxPoolService _txPoolService;
        
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
        

        public Miner(IBlockGenerationService blockGenerationService, IMinerConfig config, 
            ITxPoolService txPoolService)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _txPoolService = txPoolService;
        }

        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        private async Task Mine()
        {
            while (!Cts.IsCancellationRequested)
            {
                MiningResetEvent.WaitOne();
                if (Cts.IsCancellationRequested) break;

                var ready = _txPoolService.GetReadyTxsAsync(Config.TxCountLimit);
                // TODO：dispatch txs with ISParallel, return collection of tx results
                List<TransactionResult> txResultList = null;
                
                // generate block
                var block = await _blockGenerationService.BlockGeneration(Config.ChainId, txResultList);
                
                // reset Promotable and update account context
                await _txPoolService.ResetAndUpdate(txResultList);
            }
        }

        /// <summary>
        /// start mining  
        /// </summary>
        public void Start()
        {
            Cts = new CancellationTokenSource();
            MiningResetEvent = new AutoResetEvent(false);
            Task.Factory.StartNew(async () => await Mine());
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
                MiningResetEvent.Dispose();
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