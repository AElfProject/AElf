using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel.Consensus;
using AElf.Miner.Miner;
using Easy.MessageHub;
using NLog;
using NLog.Fluent;
using NServiceKit.Common.Extensions;
using ITxPoolService = AElf.ChainController.TxMemPool.ITxPoolService;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;
using Status = AElf.Kernel.Status;

// ReSharper disable once CheckNamespace
namespace AElf.Miner.Miner
{
    [LoggerName(nameof(Miner))]
    public class Miner : IMiner
    {
        private readonly ITxPoolService _txPoolService;
        private ECKeyPair _keyPair;
        private readonly IChainService _chainService;
        private readonly IStateDictator _stateDictator;
        private readonly ISmartContractService _smartContractService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainManagerBasic _chainManagerBasic;
        private readonly IBlockManagerBasic _blockManagerBasic;

        private MinerLock Lock { get; } = new MinerLock();
        private readonly ILogger _logger;
        private IBlockChain _blockChain;
        
        
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        public CancellationTokenSource Cts { get; private set; }

        public IMinerConfig Config { get; }

        public Hash Coinbase => Config.CoinBase;

        public Miner(IMinerConfig config, ITxPoolService txPoolService,  IChainService chainService, 
            IStateDictator stateDictator,  ISmartContractService smartContractService, 
            IExecutingService executingService, ITransactionManager transactionManager, 
            ITransactionResultManager transactionResultManager, ILogger logger, 
            IChainCreationService chainCreationService, IChainManagerBasic chainManagerBasic, 
            IBlockManagerBasic blockManagerBasic)
        {
            Config = config;
            _txPoolService = txPoolService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _smartContractService = smartContractService;
            _executingService = executingService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _logger = logger;
            _chainCreationService = chainCreationService;
            _chainManagerBasic = chainManagerBasic;
            _blockManagerBasic = blockManagerBasic;
            var chainId = config.ChainId;
            _stateDictator.ChainId = chainId;
        }
        
        private static Miners Miners
        {
            get
            {
                var dict = MinersConfig.Instance.Producers;
                var miners = new Miners();

                foreach (var bp in dict.Values)
                {
                    var b = bp["address"].RemoveHexPrefix();
                    miners.Nodes.Add(b);
                }

                Globals.BlockProducerNumber = miners.Nodes.Count;
                return miners;
            }
        }

        public async Task<IBlock> Mine(int timeoutMilliseconds, bool initial = false)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var timer = new Timer(s => cancellationTokenSource.Cancel()))
            {
                timer.Change(timeoutMilliseconds, Timeout.Infinite);
                try
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return null;

                    var readyTxs = await _txPoolService.GetReadyTxsAsync();
                    // TODO：dispatch txs with ISParallel, return list of tx results

                    // reset Promotable and update account context

                    _logger?.Log(LogLevel.Debug, "Executing Transactions..");
                    var traces = readyTxs.Count == 0
                        ? new List<TransactionTrace>()
                        : await _executingService.ExecuteAsync(readyTxs, Config.ChainId, cancellationTokenSource.Token);
                    _logger?.Log(LogLevel.Debug, "Executed Transactions.");
                    
                    // transaction results
                    ExtractTransactionResults(readyTxs, traces, out var executed, out var rollback, out var results);
                    var addrs = executed.Select(t => t.From).ToHashSet();

                    // update tx pool state
                    await _txPoolService.UpdateAccountContext(addrs);
                    
                    // generate block
                    var block = await GenerateBlockAsync(Config.ChainId, results);
                    _logger?.Log(LogLevel.Debug, $"Generated Block at height {block.Header.Index}");
                    
                    // sign block
                    block.Sign(_keyPair);

                    // broadcast
                    MessageHub.Instance.Publish(new BlockMinedMessage(block));
                    
                    // append block
                    await _blockChain.AddBlocksAsync(new List<IBlock> {block});
                    // put back canceled transactions
                    // No await so that it won't affect Consensus
                    _txPoolService.RollBack(rollback);
                    // insert txs to db
                    InsertTxs(executed, results, block);
                    
                    return block;
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Mining failed with exception.");
                    return null;
                }
            }
        }

        /// <summary>
        /// extract tx results from traces
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="traces"></param>
        /// <param name="executed"></param>
        /// <param name="rollback"></param>
        /// <param name="results"></param>
        private void ExtractTransactionResults(IEnumerable<Transaction> readyTxs, IEnumerable<TransactionTrace> traces, 
            out List<Transaction> executed, out List<Transaction> rollback, out List<TransactionResult> results)
        {
            var canceledTxIds = new List<Hash>();
            results = new List<TransactionResult>();
            foreach (var trace in traces)
            {
                switch (trace.ExecutionStatus)
                {
                    case ExecutionStatus.Canceled:
                        // Put back transaction
                        canceledTxIds.Add(trace.TransactionId);
                        break;
                    case ExecutionStatus.ExecutedAndCommitted:
                        // Successful
                        var txRes = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            Status = Status.Mined,
                            RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes())
                        };
                        txRes.UpdateBloom();
                        results.Add(txRes);
                        break;
                    case ExecutionStatus.ContractError:
                        var txResF = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            Status = Status.Failed
                        };
                        results.Add(txResF);
                        break;
                    case ExecutionStatus.Undefined:
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status Undefined. Transaction trace: {trace}""");
                        break;
                    case ExecutionStatus.SystemError:
                        // SystemError shouldn't happen, and need to fix
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status SystemError. Transaction trace: {trace}""");
                        break;
                    case ExecutionStatus.ExecutedButNotCommitted:
                        // If this happens, there's problem with the code
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status ExecutedButNotCommitted. Transaction trace: {
                                    trace
                                }""");
                        break;
                }
            }
            
            var canceled = canceledTxIds.ToHashSet();
            executed = new List<Transaction>();
            rollback = new List<Transaction>();
            foreach (var tx in readyTxs)
            {
                if (canceled.Contains(tx.GetHash()))
                {
                    rollback.Add(tx);
                }
                else
                {
                    executed.Add(tx);
                }
            }
        }
        
        /// <summary>
        /// update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        private async Task InsertTxs(List<Transaction> executedTxs, List<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            executedTxs.AsParallel().ForEach(async tx =>
                {
                    await _transactionManager.AddTransactionAsync(tx);
                    _txPoolService.RemoveAsync(tx.GetHash());
                });
            txResults.ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
        }
        
        /// <summary>
        /// generate block
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private async Task<IBlock> GenerateBlockAsync(Hash chainId, List<TransactionResult> results)
        {
            var blockChain = _chainService.GetBlockChain(chainId);

            var currentBlockHash = await blockChain.GetCurrentBlockHashAsync();
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            var block = new Block(currentBlockHash)
            {
                Header =
                {
                    Index = index,
                    ChainId = chainId,
                    Bloom = ByteString.CopyFrom(
                        Bloom.AndMultipleBloomBytes(
                            results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                        )
                    )
                }
            };

            // add tx hash
            block.AddTransactions(results.Select(r => r.TransactionId));
            
            // set ws merkle tree root
            await _stateDictator.SetWorldStateAsync();
            var ws = await _stateDictator.GetLatestWorldStateAsync();
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);

            if (ws != null)
            {
                block.Header.MerkleTreeRootOfWorldState = await ws.GetWorldStateMerkleTreeRootAsync();
            }
            // calculate and set tx merkle tree root 
            block.Complete();
            
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
            var blockChain = _chainService.GetBlockChain(chainId);
            
            var lastBlockHash = await blockChain.GetCurrentBlockHashAsync();
            // TODO: Generic IBlockHeader
            var lastHeader = (BlockHeader) await blockChain.GetHeaderByHashAsync(lastBlockHash);
            var index = lastHeader.Index;
            var block = new Block(lastBlockHash);
            block.Header.Index = index + 1;
            block.Header.ChainId = chainId;
            
            
            var ws = await _stateDictator.GetWorldStateAsync(lastBlockHash);
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
        public void Start(ECKeyPair nodeKeyPair)
        {
//            Cts = new CancellationTokenSource();
            _keyPair = nodeKeyPair;
            _blockChain = _chainService.GetBlockChain(Config.ChainId);
        }

        /// <summary>
        /// stop mining
        /// </summary>
        public void Stop()
        {
            Lock.WriteLock(() =>
            {
//                Cts.Cancel();
//                Cts.Dispose();
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