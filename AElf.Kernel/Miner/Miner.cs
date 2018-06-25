using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.Miner
{
    [LoggerName("Node")]
    public class Miner : IMiner
    {
        private readonly ITxPoolService _txPoolService;
        private ECKeyPair _keyPair;
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly IWorldStateManager _worldStateManager;
        private ISmartContractService _smartContractService;


        private readonly Dictionary<ulong, IBlock> waiting = new Dictionary<ulong, IBlock>();

        private MinerLock Lock { get; } = new MinerLock();
        
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }


        private IParallelTransactionExecutingService _parallelTransactionExecutingService;

        
        /// <summary>
        /// event set to mine
        /// </summary>
        public AutoResetEvent MiningResetEvent { get; private set; }
        
        public IMinerConfig Config { get; }

        public Hash Coinbase => Config.CoinBase;

        public Miner(IMinerConfig config, ITxPoolService txPoolService, 
                IChainManager chainManager, IBlockManager blockManager, IWorldStateManager worldStateManager, 
            ISmartContractService smartContractService)
        {
            Config = config;
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _worldStateManager = worldStateManager;
            _smartContractService = smartContractService;
        }

        
        public async Task<IBlock> Mine()
        {
            try
            {
                if (Cts == null || Cts.IsCancellationRequested)
                    return null;            

                var ready = await _txPoolService.GetReadyTxsAsync(Config.TxCount);
                // TODO：dispatch txs with ISParallel, return list of tx results

                // reset Promotable and update account context
            
                List<TransactionTrace> traces = null;
                if(Config.IsParallel)
                {  
                    traces = ready.Count == 0
                    ? new List<TransactionTrace>()
                    : await _parallelTransactionExecutingService.ExecuteAsync(ready, Config.ChainId);
                }
                else
                {
                    foreach (var transaction in ready)
                    {
                        var executive = await _smartContractService.GetExecutiveAsync(transaction.To, Config.ChainId);
                        var txnInitCtxt = new TransactionContext()
                        {
                            Transaction = transaction
                        };
                        await executive.SetTransactionContext(txnInitCtxt).Apply(true);
                    }
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
                        res.RetVal = trace.RetVal;
                    }
                    else
                    {
                        res.Status = Status.Failed;
                    }
                    results.Add(res);
                }
            
                // generate block
                var block = await GenerateBlockAsync(Config.ChainId, results);
            
                await _txPoolService.ResetAndUpdate(results);
                // sign block
                ECSigner signer = new ECSigner();
                var hash = block.GetHash();
                var bytes = hash.GetHashBytes();
                ECSignature signature = signer.Sign(_keyPair, bytes);

                block.Header.P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded());
                block.Header.R = ByteString.CopyFrom(signature.R);
                block.Header.S = ByteString.CopyFrom(signature.S);
            
                // append block
                await _blockManager.AddBlockAsync(block);
                await _chainManager.AppendBlockToChainAsync(block);
            
            
                return block;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        
        /// <summary>
        /// generate block
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public async Task<IBlock> GenerateBlockAsync(Hash chainId, IEnumerable<TransactionResult> results)
        {
            
            var lastBlockHash = await _chainManager.GetChainLastBlockHash(chainId);
            var index = await _chainManager.GetChainCurrentHeight(chainId);
            var block = new Block(lastBlockHash);
            block.Header.Index = index;
            block.Header.ChainId = chainId;

            // add tx hash
            foreach (var r in results)
            {
                block.AddTransaction(r.TransactionId);
            }
        
            // calculate and set tx merkle tree root
            block.FillTxsMerkleTreeRootInHeader();
            
            
            // set ws merkle tree root
            await _worldStateManager.OfChain(chainId);
            
            await _worldStateManager.SetWorldStateAsync(lastBlockHash);
            var ws = await _worldStateManager.GetWorldStateAsync(lastBlockHash);
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            

            if(ws != null)
                block.Header.MerkleTreeRootOfWorldState = await ws.GetWorldStateMerkleTreeRootAsync();
            block.Body.BlockHeader = block.Header.GetHash();

            
            return block;
        }
        
        
        /// <summary>
        /// generate block header
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="merkleTreeRootForTransaction"></param>
        /// <returns></returns>
        public async Task<IBlockHeader> GenerateBlockHeaderAsync(Hash chainId, Hash merkleTreeRootForTransaction)
        {
            // get ws merkle tree root
            var lastBlockHash = await _chainManager.GetChainLastBlockHash(chainId);
            var index = await _chainManager.GetChainCurrentHeight(chainId);
            var block = new Block(lastBlockHash);
            block.Header.Index = index + 1;
            block.Header.ChainId = chainId;
            
            
            await _worldStateManager.OfChain(chainId);
            var ws = await _worldStateManager.GetWorldStateAsync(lastBlockHash);
            var state = await ws.GetWorldStateMerkleTreeRootAsync();
            
            var header = new BlockHeader
            {
                Version = 0,
                PreviousBlockHash = lastBlockHash,
                MerkleTreeRootOfWorldState = state,
                MerkleTreeRootOfTransactions = merkleTreeRootForTransaction
            };

            return header;

        }
        

        
        /// <summary>
        /// start mining  
        /// </summary>
        public void Start(ECKeyPair nodeKeyPair, IParallelTransactionExecutingService parallelTransactionExecutingService)
        {
            Cts = new CancellationTokenSource();
            _keyPair = nodeKeyPair;
            _parallelTransactionExecutingService = parallelTransactionExecutingService;
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