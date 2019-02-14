using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Account;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Execution;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Miner
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger {get;set;}
        private readonly ITxHub _txHub;
        private readonly IChainService _chainService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private IBlockChain _blockChain;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private ITransactionFilter _txFilter;
        private readonly IAccountService _accountService;
        private readonly IBlockchainStateManager _blockchainStateManager;

        private const float RatioMine = 0.3f;

        public MinerService(ITxHub txHub, IChainService chainService,
            IExecutingService executingService, ITransactionResultManager transactionResultManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager,
            ITransactionFilter transactionFilter, IAccountService accountService, IBlockGenerationService blockGenerationService, 
            ISystemTransactionGenerationService systemTransactionGenerationService, IBlockchainStateManager blockchainStateManager)
        {
            _txHub = txHub;
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            Logger = NullLogger<MinerService>.Instance;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockchainStateManager = blockchainStateManager;
            _txFilter = transactionFilter;
            _accountService = accountService;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<IBlock> Mine(int chainId)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                _blockChain = _chainService.GetBlockChain(chainId);

                // generate block without txns
                var block = await GenerateBlock(chainId, await _blockChain.GetCurrentBlockHashAsync(),
                    await _blockChain.GetCurrentBlockHeightAsync());
                
                await GenerateSystemTransactions(chainId);
                DateTime currentBlockTime = block.Header.Time.ToDateTime();
                var txs = await _txHub.GetReceiptsOfExecutablesAsync();
                var txGrp = txs.GroupBy(tr => tr.IsSystemTxn).ToDictionary(x => x.Key, x => x.ToList());
                var traces = new List<TransactionTrace>();
                if (txGrp.TryGetValue(true, out var sysRcpts))
                {
                    var sysTxs = sysRcpts.Select(x => x.Transaction).ToList();
                    _txFilter.Execute(sysTxs);
                    Logger.LogTrace($"Start executing {sysTxs.Count} system transactions.");
                    traces = await ExecuteTransactions(chainId, sysTxs, currentBlockTime,true, TransactionType.DposTransaction);
                    Logger.LogTrace($"Finish executing {sysTxs.Count} system transactions.");
                }
                if (txGrp.TryGetValue(false, out var regRcpts))
                {
                    var contractZeroAddress = ContractHelpers.GetGenesisBasicContractAddress(chainId);
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
                    traces.AddRange(await ExecuteTransactions(chainId, regTxs, currentBlockTime));
                    Logger.LogTrace($"Finish executing {regTxs.Count} regular transactions.");
                    
                    Logger.LogTrace($"Start executing {contractTxs.Count} contract transactions.");
                    traces.AddRange(await ExecuteTransactions(chainId, contractTxs, currentBlockTime,
                        transactionType: TransactionType.ContractDeployTransaction));
                    Logger.LogTrace($"Finish executing {contractTxs.Count} contract transactions.");
                }

                ExtractTransactionResults(chainId, traces, out var results);

                // complete block
                await CompleteBlock(block, results);
                Logger.LogInformation($"Generated block {block.BlockHashToHex} at height {block.Header.Height} with {block.Body.TransactionsCount} txs.");

                // validate block before appending
//                var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block);
//                if (blockValidationResult != BlockValidationResult.Success)
//                {
//                    Logger.LogWarning($"Found the block generated before invalid: {blockValidationResult}.");
//                    return null;
//                }

                var blockStateSet = new BlockStateSet()
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height,
                    PreviousHash = block.Header.PreviousBlockHash
                };
                FillBlockStateSet(blockStateSet, traces);
                await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
                
                // append block
                await _blockChain.AddBlocksAsync(new List<IBlock> {block});

                MessageHub.Instance.Publish(new BlockMined(block));

                // insert to db
                UpdateStorage(chainId, results, block);
                
                await _txHub.OnNewBlock((Block)block);
                
                MessageHub.Instance.Publish(UpdateConsensus.UpdateAfterMining); 
                
                stopwatch.Stop();
                
                Logger.LogInformation($"Generate block {block.BlockHashToHex} at height {block.Header.Height} " +
                              $"with {block.Body.TransactionsCount} txs, duration {stopwatch.ElapsedMilliseconds} ms.");

                return block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Mining failed with exception.");
                return null;
            }
        }

        private async Task GenerateSystemTransactions(int chainId)
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
                await SignAndInsertToPool(chainId, txn);
            }
        }

        private async Task SignAndInsertToPool(int chainId, Transaction notSignerTransaction)
        {
            if (notSignerTransaction.Sigs.Count > 0)
                return;
            // sign tx
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Sigs.Add(ByteString.CopyFrom(signature));
            await InsertTransactionToPool(chainId, notSignerTransaction);
        }

        private async Task<List<TransactionTrace>> ExecuteTransactions(int chainId, List<Transaction> txs, DateTime currentBlockTime, bool noTimeout = false,
            TransactionType transactionType = TransactionType.ContractTransaction)
        {
            using (var cts = new CancellationTokenSource())
            {
                if (!noTimeout)
                {
                    // todo: use 4000 ms and change this later.
                    //var distance = await _consensusDataProvider.GetDistanceToTimeSlotEnd();
                    var distance = 4000;

                    var timeout = distance *  RatioMine;
                    cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                    Logger.LogTrace($"Execution limit time: {timeout}ms");
                }

                if (cts.IsCancellationRequested)
                    return null;
                var disambiguationHash =
                    HashHelpers.GetDisambiguationHash(await GetNewBlockIndexAsync(chainId), Hash.FromRawBytes(await _accountService.GetPublicKeyAsync()));

                var traces = txs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(txs, chainId, currentBlockTime, cts.Token, disambiguationHash, transactionType);

                return traces;
            }
        }

        private async Task<ulong> GetNewBlockIndexAsync(int chainId)
        {
            var blockChain = _chainService.GetBlockChain(chainId);
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            return index;
        }

        private async Task InsertTransactionToPool(int chainId, Transaction tx, bool skipValidation = true)
        {
            if (tx == null)
                return;
            // insert to tx pool and broadcast
            await _txHub.AddTransactionAsync(chainId, tx, skipValidation: skipValidation);
        }

        private void FillBlockStateSet(BlockStateSet blockStateSet,IEnumerable<TransactionTrace> traces)
        {
            foreach (var trace in traces)
            {
                foreach (var w in trace.GetFlattenedWrite())
                {
                    blockStateSet.Changes[w.Key] = w.Value;    
                }
            }
        }

        /// <summary>
        /// Extract tx results from traces
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="traces"></param>
        /// <param name="results"></param>
        private void ExtractTransactionResults(int chainId, IEnumerable<TransactionTrace> traces, out HashSet<TransactionResult> results)
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
                                Status = TransactionResultStatus.Mined,
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
                                InsertTransactionToPool(chainId, deferredTxn, false).ConfigureAwait(false);
                                txRes.DeferredTxnId = deferredTxn.GetHash();
                            }

                            results.Add(txRes);
                            break;
                        case ExecutionStatus.ContractError:
                            var txResF = new TransactionResult()
                            {
                                TransactionId = trace.TransactionId,
                                RetVal = ByteString.CopyFromUtf8(trace.StdErr), // Is this needed?
                                Status = TransactionResultStatus.Failed,
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
                                Status = TransactionResultStatus.Failed,
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
        /// <param name="chainId"></param>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private void UpdateStorage(int chainId, HashSet<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Height;
            var bh = block.Header.GetHash();
            txResults.AsParallel().ToList().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
            // update merkle tree
            _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree, chainId,
                block.Header.Height);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <returns></returns>
        private async Task<Block> GenerateBlock(int chainId, Hash preBlockHash, ulong preBlockHeight)
        {
            var block = await _blockGenerationService.GenerateBlockAsync(new GenerateBlockDto
            {
                ChainId = chainId,
                PreviousBlockHash = preBlockHash,
                PreviousBlockHeight = preBlockHeight
            });
            return block;
        }

        private async Task CompleteBlock(Block block, HashSet<TransactionResult> results)
        {
            _blockGenerationService.FillBlockAsync(block, results);
            var publicKey = await _accountService.GetPublicKeyAsync();
            block.Sign(publicKey, data => _accountService.SignAsync(data));
        }
        
    }
}