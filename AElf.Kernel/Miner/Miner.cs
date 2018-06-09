using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        
        
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
            IBlockVaildationService blockVaildationService, IChainContextService chainContextService)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _txPoolService = txPoolService;
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
        }

        
        public async Task<IBlock> Mine()
        {
            if (Cts == null || Cts.IsCancellationRequested)
                return null;
            
            var ready = await _txPoolService.GetReadyTxsAsync(Config.TxCount);
            // TODO：dispatch txs with ISParallel, return list of tx results
            
            var results =  await _parallelTransactionExecutingService.ExecuteAsync(ready, Config.ChainId);
            
            // generate block
            var block = await _blockGenerationService.GenerateBlockAsync(Config.ChainId, results);
            
            
            // reset Promotable and update account context
            await _txPoolService.ResetAndUpdate(results);

            return block;
        }

        
        public async Task SynchronizeBlock(IBlock block)
        {
            var context = await _chainContextService.GetChainContextAsync(Config.ChainId);
            await _blockVaildationService.ValidateBlockAsync(block, context);
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