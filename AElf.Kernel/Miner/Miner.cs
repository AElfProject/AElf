using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.Miner
{
    public class Miner : IMiner
    {
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ITxPoolService _txPoolService;
        private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService;
        private ECKeyPair _keyPair;
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
            ITxPoolService txPoolService, IParallelTransactionExecutingService parallelTransactionExecutingService)
        {
            _blockGenerationService = blockGenerationService;
            Config = config;
            _txPoolService = txPoolService;
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
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
                    res.Status = Status.Mined;
                }
                else
                {
                    res.Status = Status.Failed;
                }
                results.Add(res);
            }
            
            // reset Promotable and update account context
            
            
            // TODO: commit tx results
            
            // generate block
            var block = await _blockGenerationService.GenerateBlockAsync(Config.ChainId, results);
            
            await _txPoolService.ResetAndUpdate(results);
            // sign block
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(_keyPair, block.GetHash().GetHashBytes());

            block.Header.P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded());
            block.Header.R = ByteString.CopyFrom(signature.R);
            block.Header.S = ByteString.CopyFrom(signature.S);
            
            return block;
        }

        
        /// <summary>
        /// start mining  
        /// </summary>
        public void Start(ECKeyPair nodeKeyPair)
        {
            Cts = new CancellationTokenSource();
            _keyPair = nodeKeyPair;
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
                _keyPair = null;
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