using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
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
using AElf.Kernel.Types.Transaction;

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
        private IBlockChain _blockChain;
        private readonly CrossChainIndexingTransactionGenerator _crossChainIndexingTransactionGenerator;
        private ECKeyPair _keyPair;
        private readonly ConsensusDataProvider _consensusDataProvider;
        private BlockGenerator _blockGenerator;
        private IMinerConfig Config { get; }
        private TransactionFilter _txFilter;
        private readonly double _maxMineTime;

        public Miner(IMinerConfig config, ITxHub txHub, IChainService chainService,
            IExecutingService executingService, ITransactionResultManager transactionResultManager,
            ILogger logger, ClientManager clientManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ServerManager serverManager,
            IBlockValidationService blockValidationService, IStateManager stateManager)
        {
            _txHub = txHub;
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _logger = logger;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _blockValidationService = blockValidationService;
            Config = config;
            _consensusDataProvider = new ConsensusDataProvider(stateManager);
            _maxMineTime = ConsensusConfig.Instance.DPoSMiningInterval * NodeConfig.Instance.RatioMine;
            _crossChainIndexingTransactionGenerator = new CrossChainIndexingTransactionGenerator(clientManager,
                serverManager);
        }
        
        /// <summary>
        /// Initializes the mining with the producers key pair.
        /// </summary>
        public void Init()
        {
            _txFilter = new TransactionFilter();
            _keyPair = NodeConfig.Instance.ECKeyPair;
            _blockChain = _chainService.GetBlockChain(Config.ChainId);
            _blockGenerator = new BlockGenerator(_chainService, Config.ChainId);
            
            MessageHub.Instance.Subscribe<NewLibFound>(newFoundLib => { LibHeight = newFoundLib.Height; });
        }

        private ulong LibHeight { get; set; }

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
                
                var currHeight = await _blockChain.GetCurrentBlockHeightAsync();
                var bn = currHeight > 4 ? currHeight - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await _blockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                if (!UnitTestDetector.IsInUnitTest)
                {
                    await GenerateClaimFeesTransaction(currHeight, bn, bhPref);    
                }
                // generate txns for cross chain indexing if possible
                await GenerateCrossTransaction(bn, bhPref);
                DateTime currentBlockTime = DateTime.UtcNow;
                var txs = await _txHub.GetReceiptsOfExecutablesAsync();
                var txGrp = txs.GroupBy(tr => tr.IsSystemTxn).ToDictionary(x => x.Key, x => x.ToList());
                var traces = new List<TransactionTrace>();
                Hash sideChainTransactionsRoot = null;
                if (txGrp.TryGetValue(true, out var sysRcpts))
                {
                    var sysTxs = sysRcpts.Select(x => x.Transaction).ToList();
                    _txFilter.Execute(sysTxs);
                    // TODO: Remove useless consensus txs.
                    _logger?.Trace($"Start executing {sysTxs.Count} system transactions.");
                    traces = await ExecuteTransactions(sysTxs, currentBlockTime,true, TransactionType.DposTransaction);
                    _logger?.Trace($"Finish executing {sysTxs.Count} system transactions.");
                    
                    // need check result of cross chain transaction 
                    sideChainTransactionsRoot = ExtractSideChainTransactionRoot(sysTxs, traces);
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
                    traces.AddRange(await ExecuteTransactions(regTxs, currentBlockTime));
                    _logger?.Trace($"Finish executing {regTxs.Count} regular transactions.");
                    
                    _logger?.Trace($"Start executing {contractTxs.Count} contract transactions.");
                    traces.AddRange(await ExecuteTransactions(contractTxs, currentBlockTime,
                        transactionType: TransactionType.ContractDeployTransaction));
                    _logger?.Trace($"Finish executing {contractTxs.Count} contract transactions.");
                }

                ExtractTransactionResults(traces, out var results);

                // generate block
                var block = await GenerateBlock(results, sideChainTransactionsRoot, currentBlockTime);
                _logger?.Info($"Generated block {block.BlockHashToHex} at height {block.Header.Index} with {block.Body.TransactionsCount} txs.");

                // validate block before appending
                var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block);
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
                
                MessageHub.Instance.Publish(UpdateConsensus.UpdateAfterMining); 
                
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

        private async Task GenerateClaimFeesTransaction(ulong prevHeight, ulong refBlockHeight, byte[] refBlockPrefix)
        {
            var address = Address.FromPublicKey(_keyPair.PublicKey);
            var tx = new Transaction()
            {
                From = address,
                To = ContractHelpers.GetTokenContractAddress(Config.ChainId),
                MethodName = "ClaimTransactionFees",
                RefBlockNumber = refBlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(prevHeight))
            };
            await SignAndInsertToPool(tx);
        }

        /// <summary>
        /// Generate transactions for cross chain indexing.
        /// </summary>
        /// <returns></returns>
        private async Task GenerateCrossTransaction(ulong refBlockHeight, byte[] refBlockPrefix)
        {
            // Do not index cross chain information if no LIB found.
            if (LibHeight <= GlobalConfig.GenesisBlockHeight)
                return;
            
            var address = Address.FromPublicKey(_keyPair.PublicKey);
            var txnForIndexingSideChain = await _crossChainIndexingTransactionGenerator.GenerateTransactionForIndexingSideChain(address, refBlockHeight,
                    refBlockPrefix);
            if (txnForIndexingSideChain != null)
            {
                _logger.Debug($"txnForIndexingSideChain {txnForIndexingSideChain.GetHash()}");
                await SignAndInsertToPool(txnForIndexingSideChain);
            }
                
            var txnForIndexingParentChain =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionForIndexingParentChain(address, refBlockHeight,
                    refBlockPrefix);
            if (txnForIndexingParentChain != null)
            {
                _logger.Debug($"txnForIndexingParentChain {txnForIndexingParentChain.GetHash()}");
                await SignAndInsertToPool(txnForIndexingParentChain);
            }
                
        }

        /// <summary>
        /// Extract side chain indexing result from transaction traces.
        /// </summary>
        /// <returns>
        /// Merkle tree root of side chain block transaction roots.
        /// </returns>
        private Hash ExtractSideChainTransactionRoot(IEnumerable<Transaction> sysTxs, List<TransactionTrace> sysTxnTraces)
        {
            if (sysTxnTraces == null) throw new ArgumentNullException(nameof(sysTxnTraces));
            var crossChainIndexingSideChainTransaction =
                sysTxs.FirstOrDefault(t => t.IsIndexingSideChainTransaction());
            if (crossChainIndexingSideChainTransaction == null)
            {
                return null;
            }
            var txHash = crossChainIndexingSideChainTransaction.GetHash();
            var sideChainIndexingTxnTrace = sysTxnTraces.FirstOrDefault(trace =>
                trace.TransactionId.Equals(txHash) &&
                trace.ExecutionStatus == ExecutionStatus.ExecutedAndCommitted);
            return  sideChainIndexingTxnTrace != null
                ? Hash.LoadByteArray(sideChainIndexingTxnTrace.RetVal.ToFriendlyBytes())
                : null;
        }
            

        private async Task SignAndInsertToPool(Transaction notSignerTransaction)
        {
            if (notSignerTransaction.Sigs.Count > 0)
                return;
            // sign tx
            var signature = new ECSigner().Sign(_keyPair, notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            await InsertTransactionToPool(notSignerTransaction);
        }

        private async Task<List<TransactionTrace>> ExecuteTransactions(List<Transaction> txs, DateTime currentBlockTime, bool noTimeout = false,
            TransactionType transactionType = TransactionType.ContractTransaction)
        {
            using (var cts = new CancellationTokenSource())
            {
                if (!noTimeout)
                {
                    var distance = await _consensusDataProvider.GetDistanceToTimeSlotEnd();
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
                    : await _executingService.ExecuteAsync(txs, Config.ChainId, currentBlockTime, cts.Token, disambiguationHash, transactionType);

                return traces;
            }
        }

        private async Task<ulong> GetNewBlockIndexAsync()
        {
            var blockChain = _chainService.GetBlockChain(Config.ChainId);
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            return index;
        }

        private async Task InsertTransactionToPool(Transaction tx, bool skipValidation = true)
        {
            if (tx == null)
                return;
            // insert to tx pool and broadcast
            await _txHub.AddTransactionAsync(tx, skipValidation: skipValidation);
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
                                Index = index++,
                                Logs = {trace.FlattenedLogs}
                            };
                            txRes.UpdateBloom();

                            // insert deferred txn to transaction pool and wait for execution 
                            if (trace.DeferredTransaction.Length != 0)
                            {
                                var deferredTxn = Transaction.Parser.ParseFrom(trace.DeferredTransaction);
                                InsertTransactionToPool(deferredTxn, false).ConfigureAwait(false);
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
                        case ExecutionStatus.InsufficientTransactionFees:
                            var txResITF = new TransactionResult()
                            {
                                TransactionId = trace.TransactionId,
                                RetVal = ByteString.CopyFromUtf8(trace.ExecutionStatus.ToString()), // Is this needed?
                                Status = Status.Failed,
                                StateHash = trace.GetSummarizedStateHash(),
                                Index = index++
                            };
                            results.Add(txResITF);
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
            txResults.AsParallel().ToList().ForEach(async r =>
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
        /// <param name="currentBlockTime"></param>
        /// <returns></returns>
        private async Task<IBlock> GenerateBlock(HashSet<TransactionResult> results, Hash sideChainTransactionsRoot,  
            DateTime currentBlockTime)
        {
            var block = await _blockGenerator.GenerateBlockAsync(results, sideChainTransactionsRoot, currentBlockTime);
            block.Sign(_keyPair);
            return block;
        }
    }
}