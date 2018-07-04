using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
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
        private readonly IWorldStateDictator _worldStateDictator;
        private ISmartContractService _smartContractService;
        private IConcurrencyExecutingService _concurrencyExecutingService;


        private readonly Dictionary<ulong, IBlock> waiting = new Dictionary<ulong, IBlock>();

        private MinerLock Lock { get; } = new MinerLock();
        
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }


        private IGrouper _grouper;

        
        /// <summary>
        /// event set to mine
        /// </summary>
        public AutoResetEvent MiningResetEvent { get; private set; }
        
        public IMinerConfig Config { get; }

        public Hash Coinbase => Config.CoinBase;

        public Miner(IMinerConfig config, ITxPoolService txPoolService, 
                IChainManager chainManager, IBlockManager blockManager, IWorldStateDictator worldStateDictator, 
            ISmartContractService smartContractService, IConcurrencyExecutingService concurrencyExecutingService)
        {
            Config = config;
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
            _smartContractService = smartContractService;
            _concurrencyExecutingService = concurrencyExecutingService;
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
                    : await _concurrencyExecutingService.ExecuteAsync(ready, Config.ChainId, _grouper);
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
                        res.RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes());
                    }
                    else
                    {
                        res.Status = Status.Failed;
                        res.RetVal = ByteString.CopyFromUtf8(trace.StdErr);
                    }
                    results.Add(res);
                }
            
                // generate block
                var block = await GenerateBlockAsync(Config.ChainId, results);
            
                await _txPoolService.ResetAndUpdate(results);
                // sign block
                ECSigner signer = new ECSigner();
                var hash = block.GetHash();
                var bytes = hash.GetBytes();
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
                return null;
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
            await _worldStateDictator.SetWorldStateAsync(lastBlockHash);
            var ws = await _worldStateDictator.GetWorldStateAsync(lastBlockHash);
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
            
            
            var ws = await _worldStateDictator.GetWorldStateAsync(lastBlockHash);
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
        public void Start(ECKeyPair nodeKeyPair, IGrouper grouper)
        {
            Cts = new CancellationTokenSource();
            _keyPair = nodeKeyPair;
            _grouper = grouper;
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