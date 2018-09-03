using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
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
using NLog;
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
            _stateDictator.ChainId = Config.ChainId;
            _stateDictator.BlockProducerAccountAddress = _keyPair.GetAddress();
            _stateDictator.BlockHeight = await _chainService.GetBlockChain(Config.ChainId).GetCurrentBlockHeightAsync();

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

                    var blockChain = _chainService.GetBlockChain(Config.ChainId);
                    var traces = readyTxs.Count == 0
                        ? new List<TransactionTrace>()
                        : await _executingService.ExecuteAsync(readyTxs, Config.ChainId, cancellationTokenSource.Token);
                    _logger?.Log(LogLevel.Debug, "Executed Transactions.");
                    var canceledTxIds = new List<Hash>();
                    var results = new List<TransactionResult>();
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

                    // insert txs to db
                    // update tx pool state
                    var canceled = canceledTxIds.ToHashSet();
                    var executed = new List<Transaction>();
                    var rollback = new List<Transaction>();
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

                    var addrs = await InsertTxs(executed, results);
                    await _txPoolService.UpdateAccountContext(addrs);

                    // generate block
                    var block = await GenerateBlockAsync(Config.ChainId, results);

                    block.Header.Bloom = ByteString.CopyFrom(
                        Bloom.AndMultipleBloomBytes(
                            results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                        )
                    );
                    _logger?.Log(LogLevel.Debug, $"Generated block {block.Header.Index}.");

                    // sign block
                    ECSigner signer = new ECSigner();
                    var hash = block.GetHash();
                    var bytes = hash.GetHashBytes();
                    ECSignature signature = signer.Sign(_keyPair, bytes);

                    block.Header.P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded());
                    block.Header.R = ByteString.CopyFrom(signature.R);
                    block.Header.S = ByteString.CopyFrom(signature.S);

                    // append block
                    await blockChain.AddBlocksAsync(new List<IBlock>() {block});

                    // put back canceled transactions
                    // No await so that it won't affect Consensus
                    await _txPoolService.RollBack(rollback);
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
        /// update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        private async Task<HashSet<Hash>> InsertTxs(List<Transaction> executedTxs, List<TransactionResult> txResults)
        {
            var addrs = new HashSet<Hash>();
            foreach (var t in executedTxs)
            {
                addrs.Add(t.From);
                await _transactionManager.AddTransactionAsync(t);
            }

            txResults.ForEach(async r => { await _transactionResultManager.AddTransactionResultAsync(r); });
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
            var blockChain = _chainService.GetBlockChain(chainId);

            var currentBlockHash = await blockChain.GetCurrentBlockHashAsync();
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            var block = new Block(currentBlockHash)
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
            await _stateDictator.SetWorldStateAsync();
            var ws = await _stateDictator.GetLatestWorldStateAsync();
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);

            if (ws != null)
            {
                block.Header.MerkleTreeRootOfWorldState = await ws.GetWorldStateMerkleTreeRootAsync();
            }
               
            block.Body.BlockHeader = block.Header.GetHash();

            await _stateDictator.SetBlockHashAsync(block.GetHash());
            await _stateDictator.SetStateHashAsync(block.GetHash());

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
            //MiningResetEvent = new AutoResetEvent(false);
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