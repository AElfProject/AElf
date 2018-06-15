using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel.Consensus;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Google.Protobuf.WellKnownTypes;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.Miner
{
    public class Miner : IMiner
    {
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ITxPoolService _txPoolService;
        private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService;
        
        
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

        private readonly DPoS _dpos;

        public Miner(IBlockGenerationService blockGenerationService, IMinerConfig config, 
            ITxPoolService txPoolService, IParallelTransactionExecutingService parallelTransactionExecutingService, DPoS dpos)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _txPoolService = txPoolService;
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
            _dpos = dpos;
        }

        
        public async Task<IBlock> Mine(byte[] address)
        {
            if (await _dpos.AbleToMine(address))
            {
                if (Cts == null || Cts.IsCancellationRequested)
                    return null;
            
                var ready = await _txPoolService.GetReadyTxsAsync(Config.TxCount);
                // TODO：dispatch txs with ISParallel, return list of tx results

                if (await _dpos.TimeToGenerateExtraBlock(address))
                {
                    await _txPoolService.AddTxAsync(await _dpos.GenerateEBPCalculationTransaction());
                }
                
                var results =  await _parallelTransactionExecutingService.ExecuteAsync(ready, Config.ChainId);
            
                // generate block
                var block = await _blockGenerationService.GenerateBlockAsync(Config.ChainId, results);
            
                // reset Promotable and update account context
                await _txPoolService.ResetAndUpdate(results);

                return block;
            }

            return null;
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