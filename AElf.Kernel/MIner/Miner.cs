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
        private readonly ITxPoolService _poolService;
        private readonly IChainManager _chainManager;
        
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

        public Miner(IBlockGenerationService blockGenerationService, IMinerConfig config, ITxPoolService poolService, IChainManager chainManager)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _poolService = poolService;
            _chainManager = chainManager;
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
                
                // return txs ready to be executed
                var readyTxs = await _poolService.GetReadyTxsAsync();

                // TODO：dispatch txs with ISParallel, return collection of tx results
                List<TransactionResult> txResultList;
                //var block = _blockGenerationService.BlockGeneration(ChainId, );

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

        public Hash ChainId => Config.ChainId;
    }
    
    /// <inheritdoc />
    /// <summary>
    /// A lock for managing asynchronous access to memory pool.
    /// </summary>
    public class MinerLock : ReaderWriterLock
    {
    }
}