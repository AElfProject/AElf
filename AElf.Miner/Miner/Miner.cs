using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.BlockService;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Txn;
using AElf.Kernel.Types;
using AElf.Miner.EventMessages;
using AElf.Miner.TxMemPool;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Miner.Miner
{
    
    public class Miner : IMiner, ISingletonDependency
    {
        public ILogger<Miner> Logger {get;set;}
        private readonly ITxHub _txHub;
        private readonly IChainService _chainService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly IBlockValidationService _blockValidationService;
        private IBlockChain _blockChain;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly ConsensusDataProvider _consensusDataProvider;
        private readonly IBlockGenerationService _blockGenerationService;
        private IMinerConfig Config { get; }
        private TransactionFilter _txFilter;
        private readonly double _maxMineTime;
        private readonly IAccountService _accountService;

        private const float RatioMine = 0.3f;

        public Miner(IMinerConfig config, ITxHub txHub, IChainService chainService,
            IExecutingService executingService, ITransactionResultManager transactionResultManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, IBlockValidationService blockValidationService, 
            TransactionFilter transactionFilter, ConsensusDataProvider consensusDataProvider, 
            IAccountService accountService, IBlockGenerationService blockGenerationService, 
            ISystemTransactionGenerationService systemTransactionGenerationService)
        {
            _txHub = txHub;
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            Logger = NullLogger<Miner>.Instance;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _blockValidationService = blockValidationService;
            Config = config;
            _consensusDataProvider = consensusDataProvider;
            _maxMineTime = ConsensusConfig.Instance.DPoSMiningInterval * RatioMine;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _txFilter = transactionFilter;
            _accountService = accountService;
        }
        
        /// <summary>
        /// Initializes the mining with the producers key pair.
        /// </summary>
        public void Init()
        {
            _blockChain = _chainService.GetBlockChain(Config.ChainId);
            
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
                await GenerateSystemTransactions();
                DateTime currentBlockTime = DateTime.UtcNow;
                var txs = await _txHub.GetReceiptsOfExecutablesAsync();
                var txGrp = txs.GroupBy(tr => tr.IsSystemTxn).ToDictionary(x => x.Key, x => x.ToList());
                var traces = new List<TransactionTrace>();
                if (txGrp.TryGetValue(true, out var sysRcpts))
                {
                    var sysTxs = sysRcpts.Select(x => x.Transaction).ToList();
                    _txFilter.Execute(sysTxs);
                    Logger.LogTrace($"Start executing {sysTxs.Count} system transactions.");
                    traces = await ExecuteTransactions(sysTxs, currentBlockTime,true, TransactionType.DposTransaction);
                    Logger.LogTrace($"Finish executing {sysTxs.Count} system transactions.");
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
                    
                    Logger.LogTrace($"Start executing {regTxs.Count} regular transactions.");
                    traces.AddRange(await ExecuteTransactions(regTxs, currentBlockTime));
                    Logger.LogTrace($"Finish executing {regTxs.Count} regular transactions.");
                    
                    Logger.LogTrace($"Start executing {contractTxs.Count} contract transactions.");
                    traces.AddRange(await ExecuteTransactions(contractTxs, currentBlockTime,
                        transactionType: TransactionType.ContractDeployTransaction));
                    Logger.LogTrace($"Finish executing {contractTxs.Count} contract transactions.");
                }

                ExtractTransactionResults(traces, out var results);

                // generate block
                var block = await GenerateBlock(results, currentBlockTime);
                Logger.LogInformation($"Generated block {block.BlockHashToHex} at height {block.Header.Index} with {block.Body.TransactionsCount} txs.");

                // validate block before appending
                var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block);
                if (blockValidationResult != BlockValidationResult.Success)
                {
                    Logger.LogWarning($"Found the block generated before invalid: {blockValidationResult}.");
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
                
                Logger.LogInformation($"Generate block {block.BlockHashToHex} at height {block.Header.Index} " +
                              $"with {block.Body.TransactionsCount} txs, duration {stopwatch.ElapsedMilliseconds} ms.");

                return block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Mining failed with exception.");
                return null;
            }
        }

        private async Task GenerateSystemTransactions()
        {
            var prevHeight = await _blockChain.GetCurrentBlockHeightAsync();
            var refBlockHeight = prevHeight > 4 ? prevHeight - 4 : 0;
            var bh = refBlockHeight == 0 ? Hash.Genesis : (await _blockChain.GetHeaderByHeightAsync(refBlockHeight)).GetHash();
            var refBlockPrefix = bh.Value.Where((x, i) => i < 4).ToArray();
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());

            var generatedTxns =
                _systemTransactionGenerationService.GenerateSystemTransactions(address, prevHeight, refBlockHeight,
                    refBlockPrefix);
            foreach (var txn in generatedTxns)
            {
                await SignAndInsertToPool(txn);
            }
        }

        private async Task SignAndInsertToPool(Transaction notSignerTransaction)
        {
            if (notSignerTransaction.Sigs.Count > 0)
                return;
            // sign tx
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Sigs.Add(ByteString.CopyFrom(signature));
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
                    var timeout = distance *  RatioMine;
                    cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                    Logger.LogTrace($"Execution limit time: {timeout}ms");
                }

                if (cts.IsCancellationRequested)
                    return null;
                var disambiguationHash =
                    HashHelpers.GetDisambiguationHash(await GetNewBlockIndexAsync(), Hash.FromRawBytes(await _accountService.GetPublicKeyAsync()));

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
                            Logger.LogCritical(
                                $@"Transaction Id ""{
                                        trace.TransactionId
                                    } is executed with status Undefined. Transaction trace: {trace}""");
                            break;
                        case ExecutionStatus.SystemError:
                            // SystemError shouldn't happen, and need to fix
                            Logger.LogCritical(
                                $@"Transaction Id ""{
                                        trace.TransactionId
                                    } is executed with status SystemError. Transaction trace: {trace}""");
                            break;
                        case ExecutionStatus.ExecutedButNotCommitted:
                            // If this happens, there's problem with the code
                            Logger.LogCritical(
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
                Logger.LogTrace(e, "Error in ExtractTransactionResults");
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
        /// <param name="currentBlockTime"></param>
        /// <returns></returns>
        private async Task<IBlock> GenerateBlock(HashSet<TransactionResult> results, DateTime currentBlockTime)
        {
            var block = await _blockGenerationService.GenerateBlockAsync(results, currentBlockTime);
            var publicKey = await _accountService.GetPublicKeyAsync();
            block.Sign(publicKey, data => _accountService.SignAsync(data));
            return block;
        }
    }
}