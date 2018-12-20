using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.Types.Transaction;
using AElf.Miner.EventMessages;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using AElf.Miner.Rpc.Server;
using AElf.Miner.TxMemPool;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using NServiceKit.Common.Extensions;

namespace AElf.Miner.Miner
{
    [LoggerName(nameof(Miner))]
    public class Miner : IMiner
    {
        private readonly ILogger _logger;
        private readonly ITxHub _txHub;
        private readonly IChainService _chainService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IChainContextService _chainContextService;
        private IBlockChain _blockChain;
        private readonly CrossChainIndexingTransactionGenerator _crossChainIndexingTransactionGenerator;
        private ECKeyPair _keyPair;
        private readonly DPoSInfoProvider _dpoSInfoProvider;
        private IMinerConfig Config { get; }
        private TransactionFilter _txFilter;
        private readonly double _maxMineTime;

        public Miner(IMinerConfig config, ITxHub txHub, IChainService chainService,
            IExecutingService executingService, ITransactionResultManager transactionResultManager,
            ILogger logger, ClientManager clientManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ServerManager serverManager,
            IBlockValidationService blockValidationService, IChainContextService chainContextService, 
            IStateStore stateStore)
        {
            _txHub = txHub;
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _logger = logger;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _blockValidationService = blockValidationService;
            _chainContextService = chainContextService;
            Config = config;
            _dpoSInfoProvider = new DPoSInfoProvider(stateStore);
            _maxMineTime = ConsensusConfig.Instance.DPoSMiningInterval * NodeConfig.Instance.RatioMine;
            _crossChainIndexingTransactionGenerator = new CrossChainIndexingTransactionGenerator(clientManager,
                serverManager);
        }
        
        /// <summary>
        /// Initializes the mining with the producers key pair.
        /// </summary>
        public void Init(ECKeyPair nodeKeyPair)
        {
            _txFilter = new TransactionFilter();
            _keyPair = NodeConfig.Instance.ECKeyPair;
            _blockChain = _chainService.GetBlockChain(Config.ChainId);
            
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<IBlock> Mine()
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                var bn = await _blockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await _blockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                // generate txns for cross chain indexing if possible
                await GenerateCrossTransaction(bn, bhPref);
                
                var txs = await _txHub.GetReceiptsOfExecutablesAsync();
                var txGrp = txs.GroupBy(tr => tr.IsSystemTxn).ToDictionary(x => x.Key, x => x.ToList());
                var traces = new List<TransactionTrace>();
                //ParentChainBlockInfo pcb = null; 
                Hash sideChainTransactionsRoot = null;
                byte[] indexedSideChainBlockInfo = null;
                if (txGrp.TryGetValue(true, out var sysRcpts))
                {
                    var sysTxs = sysRcpts.Select(x => x.Transaction).ToList();
                    _logger.Trace($"Before filter:");
                    foreach (var tx in sysTxs)
                    {
                        _logger.Trace($"{tx.GetHash()} - {tx.MethodName}");
                    }
                    
                    _txFilter.Execute(sysTxs);
                    _logger.Trace($"After filter:");
                    foreach (var tx in sysTxs)
                    {
                        _logger.Trace($"{tx.GetHash()} - {tx.MethodName}");
                    }

                    _logger?.Trace($"Start executing {sysTxs.Count} system transactions.");
                    traces = await ExecuteTransactions(sysTxs, true, TransactionType.DposTransaction);
                    _logger?.Trace($"Finish executing {sysTxs.Count} system transactions.");
                    
                    // need check result of cross chain transaction 
                    var crossChainIndexingSideChainTransaction =
                        sysTxs.FirstOrDefault(t => t.IsIndexingSideChainTransaction());
                    if (crossChainIndexingSideChainTransaction != null)
                    {
                        var txHash = crossChainIndexingSideChainTransaction.GetHash();
                        var sideChainIndexingTxnTrace = traces.FirstOrDefault(trace =>
                            trace.TransactionId.Equals(txHash) &&
                            trace.ExecutionStatus == ExecutionStatus.ExecutedAndCommitted);
                        sideChainTransactionsRoot = sideChainIndexingTxnTrace != null
                            ? Hash.LoadByteArray(sideChainIndexingTxnTrace.RetVal.ToFriendlyBytes())
                            : null;
                        indexedSideChainBlockInfo = sideChainTransactionsRoot != null ? crossChainIndexingSideChainTransaction
                            .Params.ToByteArray() : null;
                    }
                }
                if (txGrp.TryGetValue(false, out var regRcpts))
                {
                    var contractZeroAddress = ContractHelpers.GetGenesisBasicContractAddress(Config.ChainId);
                    var regTxs = new List<Transaction>();
                    var contractTxs = new List<Transaction>();

                    foreach (var regRcpt in regRcpts)
                    {
                        if (regRcpt.Transaction.To.Equals(contractZeroAddress))
                        {
                            contractTxs.Add(regRcpt.Transaction);
                        }
                        else
                        {
                            regTxs.Add(regRcpt.Transaction);
                        }
                    }
                    
                    _logger?.Trace($"Start executing {regTxs.Count} regular transactions.");
                    traces.AddRange(await ExecuteTransactions(regTxs));
                    _logger?.Trace($"Finish executing {regTxs.Count} regular transactions.");
                    
                    _logger?.Trace($"Start executing {contractTxs.Count} contract transactions.");
                    traces.AddRange(await ExecuteTransactions(contractTxs, transactionType: TransactionType.ContractDeployTransaction));
                    _logger?.Trace($"Finish executing {contractTxs.Count} contract transactions.");
                }

                ExtractTransactionResults(traces, out var results);

                // generate block
                var block = await GenerateBlockAsync(results, sideChainTransactionsRoot, indexedSideChainBlockInfo);
                _logger?.Info($"Generated block {block.BlockHashToHex} at height {block.Header.Index} with {block.Body.TransactionsCount} txs.");

                // validate block before appending
                var chainContext = await _chainContextService.GetChainContextAsync(Hash.LoadBase58(ChainConfig.Instance.ChainId));
                var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block, chainContext);
                if (blockValidationResult != BlockValidationResult.Success)
                {
                    _logger?.Warn($"Found the block generated before invalid: {blockValidationResult}.");
                    return null;
                }
                // append block
                await _blockChain.AddBlocksAsync(new List<IBlock> {block});

                MessageHub.Instance.Publish(new BlockMined(block));

                // insert to db
                UpdateStorage(results, block);
                await _txHub.OnNewBlock((Block)block);
                MessageHub.Instance.Publish(new BlockMinedAndStored(block));
                stopwatch.Stop();
                _logger?.Info($"Generate block {block.BlockHashToHex} at height {block.Header.Index} " +
                              $"with {block.Body.TransactionsCount} txs, duration {stopwatch.ElapsedMilliseconds} ms.");

                return block;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Mining failed with exception.");
                return null;
            }
        }

        /// <summary>
        /// Generate transactions for cross chain indexing.
        /// </summary>
        /// <returns></returns>
        private async Task GenerateCrossTransaction(ulong refBlockHeight, byte[] refBlockPrefix)
        {
            var address = Address.FromPublicKey(_keyPair.PublicKey);
            var txnForIndexingSideChain = _crossChainIndexingTransactionGenerator.GenerateTransactionForIndexingSideChain(address, refBlockHeight,
                    refBlockPrefix);
            if (txnForIndexingSideChain != null)
                await SignAndInsertToPool(txnForIndexingSideChain);

            var txnForIndexingParentChain =
                _crossChainIndexingTransactionGenerator.GenerateTransactionForIndexingParentChain(address, refBlockHeight,
                    refBlockPrefix);
            if (txnForIndexingParentChain != null)
                await SignAndInsertToPool(txnForIndexingParentChain);
        }

        private async Task SignAndInsertToPool(Transaction notSignerTransaction)
        {
            if (!notSignerTransaction.Sigs.IsEmpty())
                return;
            // sign tx
            var signature = new ECSigner().Sign(_keyPair, notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            await InsertTransactionToPool(notSignerTransaction);
        }

        private async Task<List<TransactionTrace>> ExecuteTransactions(List<Transaction> txs, bool noTimeout = false,
            TransactionType transactionType = TransactionType.ContractTransaction)
        {
            using (var cts = new CancellationTokenSource())
            {
                if (!noTimeout)
                {
                    var distance = await _dpoSInfoProvider.GetDistanceToTimeSlotEnd();
                    var distanceRation = distance * (NodeConfig.Instance.RatioSynchronize + NodeConfig.Instance.RatioMine);
                    var timeout = Math.Min(distanceRation, _maxMineTime);
                    cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                    _logger?.Trace($"Execution limit time: {timeout}ms");
                }

                if (cts.IsCancellationRequested)
                    return null;
                var disambiguationHash =
                    HashHelpers.GetDisambiguationHash(await GetNewBlockIndexAsync(), Hash.FromRawBytes(_keyPair.PublicKey));

                var traces = txs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(txs, Config.ChainId, cts.Token, disambiguationHash,transactionType);

                return traces;
            }
        }

        private async Task<ulong> GetNewBlockIndexAsync()
        {
            var blockChain = _chainService.GetBlockChain(Config.ChainId);
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            return index;
        }

        private async Task InsertTransactionToPool(Transaction tx)
        {
            if (tx == null)
                return;
            // insert to tx pool and broadcast
            await _txHub.AddTransactionAsync(tx, skipValidation: true);
        }

        /// <summary>
        /// Extract tx results from traces
        /// </summary>
        /// <param name="traces"></param>
        /// <param name="results"></param>
        private void ExtractTransactionResults(IEnumerable<TransactionTrace> traces, out HashSet<TransactionResult> results)
        {
            results = new HashSet<TransactionResult>();
            try
            {
                int index = 0;
                foreach (var trace in traces)
                {
                    switch (trace.ExecutionStatus)
                    {
                        case ExecutionStatus.Canceled:
                            // Put back transaction
                            break;
                        case ExecutionStatus.ExecutedAndCommitted:
                            // Successful
                            var txRes = new TransactionResult()
                            {
                                TransactionId = trace.TransactionId,
                                Status = Status.Mined,
                                RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes()),
                                StateHash = trace.GetSummarizedStateHash(),
                                Index = index++
                            };
                            txRes.UpdateBloom();

                            // insert deferred txn to transaction pool and wait for execution 
                            if (trace.DeferredTransaction.Length != 0)
                            {
                                var deferredTxn = Transaction.Parser.ParseFrom(trace.DeferredTransaction);
                                InsertTransactionToPool(deferredTxn).ConfigureAwait(false);
                                txRes.DeferredTxnId = deferredTxn.GetHash();
                            }

                            results.Add(txRes);
                            break;
                        case ExecutionStatus.ContractError:
                            var txResF = new TransactionResult()
                            {
                                TransactionId = trace.TransactionId,
                                RetVal = ByteString.CopyFromUtf8(trace.StdErr), // Is this needed?
                                Status = Status.Failed,
                                StateHash = Hash.Default,
                                Index = index++
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
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error in ExtractTransactionResults");
            }
        }

        /// <summary>
        /// Update database.
        /// </summary>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private void UpdateStorage(HashSet<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
            // update merkle tree
            _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree, Config.ChainId,
                block.Header.Index);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <param name="results"></param>
        /// <param name="sideChainTransactionsRoot"></param>
        /// <param name="indexedSideChainBlockInfo"></param>
        /// <returns></returns>
        private async Task<IBlock> GenerateBlockAsync(HashSet<TransactionResult> results,
            Hash sideChainTransactionsRoot, byte[] indexedSideChainBlockInfo)
        {
            var blockChain = _chainService.GetBlockChain(Config.ChainId);

            var currentBlockHash = await blockChain.GetCurrentBlockHashAsync();
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            var block = new Block(currentBlockHash)
            {
                Header =
                {
                    Index = index,
                    ChainId = Config.ChainId,
                    Bloom = ByteString.CopyFrom(
                        Bloom.AndMultipleBloomBytes(
                            results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                        )
                    ),
                    SideChainTransactionsRoot = sideChainTransactionsRoot
                }
            };

            var sideChainBlockInfo = indexedSideChainBlockInfo != null
                ? (SideChainBlockInfo[]) ParamsPacker.Unpack(indexedSideChainBlockInfo,
                    new[] {typeof(SideChainBlockInfo[])})[0]
                : null;
            // calculate and set tx merkle tree root 
            block.Complete(sideChainBlockInfo, results);
            block.Sign(_keyPair);
            return block;
        }
    }
}