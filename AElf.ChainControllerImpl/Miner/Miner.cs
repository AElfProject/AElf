﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;
using AElf.SmartContract;
using AElf.Execution;
using AElf.Kernel;
using AElf.ChainController.Execution;

namespace AElf.ChainController
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
        private ITransactionManager _transactionManager;
        private ITransactionResultManager _transactionResultManager;

        private readonly Dictionary<ulong, IBlock> waiting = new Dictionary<ulong, IBlock>();

        private MinerLock Lock { get; } = new MinerLock();
        
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }

        private IGrouper _grouper;
        
        public IMinerConfig Config { get; }

        public Hash Coinbase => Config.CoinBase;

        public Miner(IMinerConfig config, ITxPoolService txPoolService, 
                IChainManager chainManager, IBlockManager blockManager, IWorldStateDictator worldStateDictator, 
            ISmartContractService smartContractService, IConcurrencyExecutingService concurrencyExecutingService, ITransactionManager transactionManager, ITransactionResultManager transactionResultManager)
        {
            Config = config;
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
            _smartContractService = smartContractService;
            _concurrencyExecutingService = concurrencyExecutingService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
        }

        
        public async Task<IBlock> Mine()
        {
            try
            {
                if (Cts == null || Cts.IsCancellationRequested)
                    return null;            

                var readyTxs = await _txPoolService.GetReadyTxsAsync();
                // TODO：dispatch txs with ISParallel, return list of tx results

                // reset Promotable and update account context
            
                List<TransactionTrace> traces = null;
                if(Config.IsParallel)
                {  
                    traces = readyTxs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _concurrencyExecutingService.ExecuteAsync(readyTxs, Config.ChainId, _grouper);
                }
                else
                {
                    foreach (var transaction in readyTxs)
                    {
                        var executive = await _smartContractService.GetExecutiveAsync(transaction.To, Config.ChainId);
                        try
                        {
                            var txnInitCtxt = new TransactionContext()
                            {
                                Transaction = transaction
                            };
                            await executive.SetTransactionContext(txnInitCtxt).Apply(true);
                        }
                        finally
                        {
                            await _smartContractService.PutExecutiveAsync(transaction.To, executive);    
                        }

                    }
                }
                
                var results = new List<TransactionResult>();
                foreach (var trace in traces)
                {
                    var res = new TransactionResult()
                    {
                        TransactionId = trace.TransactionId
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
                        Console.WriteLine("Failed to execute tx:\n" + trace.StdErr);
                    }
                    results.Add(res);
                }
                
                // insert txs to db
                // update tx pool state
                var addrs = await InsertTxs(readyTxs, results);
                await _txPoolService.UpdateAccountContext(addrs);
            
                // generate block
                var block = await GenerateBlockAsync(Config.ChainId, results);
                
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
                return null;
            }
        }
        
        /// <summary>
        /// update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        private async Task<HashSet<Hash>> InsertTxs(List<ITransaction> executedTxs, List<TransactionResult> txResults)
        {
            var addrs = new HashSet<Hash>();
            foreach (var t in executedTxs)
            {
                addrs.Add(t.From);
                await _transactionManager.AddTransactionAsync(t);
            }
            
            txResults.ForEach(async r =>
            {
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
            return addrs;
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
            var block = new Block(lastBlockHash)
            {
                Header =
                {
                    Index = index,
                    ChainId = chainId
                }
            };

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