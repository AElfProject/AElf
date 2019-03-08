using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;
using ByteString = Google.Protobuf.ByteString;

namespace AElf.Kernel.Services
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger { get; set; }
        private ITxHub _txHub;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IAccountService _accountService;

        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;

        public ILocalEventBus EventBus { get; set; }

        private const float RatioMine = 0.3f;

        public MinerService(IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockchainService blockchainService, IBlockExecutingService blockExecutingService,
            IConsensusService consensusService, IBlockchainExecutingService blockchainExecutingService, ITxHub txHub)
        {
            Logger = NullLogger<MinerService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _consensusService = consensusService;
            _blockchainExecutingService = blockchainExecutingService;
            _txHub = txHub;
            _blockchainService = blockchainService;
            _accountService = accountService;

            EventBus = NullLocalEventBus.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, DateTime time)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // generate block without txns
            var block = await GenerateBlock(previousBlockHash, previousBlockHeight);

            var transactions = await GenerateSystemTransactions(previousBlockHash, previousBlockHeight);

            var executableTransactionSet = await _txHub.GetExecutableTransactionSetAsync();
            var pending = new List<Transaction>();
            if (executableTransactionSet.PreviousBlockHash == previousBlockHash)
            {
                pending = executableTransactionSet.Transactions;
            }
            else
            {
                Logger.LogWarning($"Transaction pool gives transactions to be appended to " +
                                  $"{executableTransactionSet.PreviousBlockHash} which doesn't match the current " +
                                  $"best chain hash {previousBlockHash}.");
            }

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(time - DateTime.UtcNow);
                block = await _blockExecutingService.ExecuteBlockAsync(block.Header,
                    transactions, pending, cts.Token);
            }

            stopwatch.Stop();
            Logger.LogInformation($"Generated {{ hash: {block.BlockHashToHex}, " +
                                  $"height: {block.Header.Height}, " +
                                  $"previous: {block.Header.PreviousBlockHash}, " +
                                  $"tx-count: {block.Body.TransactionsCount}," +
                                  $"duration: {stopwatch.ElapsedMilliseconds} ms. }}");

            /*DateTime currentBlockTime = block.Header.Time.ToDateTime();
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
                var contractZeroAddress = ContractHelpers.GetGenesisBasicContractAddress();
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

            // complete block
            await CompleteBlock(block, results);
            Logger.LogInformation($"Generated block {block.BlockHashToHex} at height {block.Header.Height} with {block.Body.TransactionsCount} txs.");

            var blockStateSet = new BlockStateSet()
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height,
                PreviousHash = block.Header.PreviousBlockHash
            };
            FillBlockStateSet(blockStateSet, traces); 
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);*/

//                await SignBlockAsync(block);
            await _blockchainService.AddBlockAsync(block);
            var chain = await _blockchainService.GetChainAsync();
            var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

            await SignBlockAsync(block);
            // TODO: TxHub needs to be updated when BestChain is found/extended, so maybe the call should be centralized
            //await _txHub.OnNewBlock(block);

            return block;
        }

        private async Task<List<Transaction>> GenerateSystemTransactions(Hash previousBlockHash,
            long previousBlockHeight)
        {
            //var previousBlockPrefix = previousBlockHash.Value.Take(4).ToArray();
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());

            var generatedTxns =
                _systemTransactionGenerationService.GenerateSystemTransactions(address, previousBlockHeight,
                    previousBlockHash);

            foreach (var txn in generatedTxns)
            {
                await SignAsync(txn);
            }

            return generatedTxns;
        }

        private async Task SignAsync(Transaction notSignerTransaction)
        {
            if (notSignerTransaction.Sigs.Count > 0)
                return;
            // sign tx
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Sigs.Add(ByteString.CopyFrom(signature));
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <returns></returns>
        private async Task<Block> GenerateBlock(Hash preBlockHash, long preBlockHeight)
        {
            var block = await _blockGenerationService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
            {
                PreviousBlockHash = preBlockHash,
                PreviousBlockHeight = preBlockHeight,
                BlockTime = DateTime.UtcNow
            });
            return block;
        }

        private async Task SignBlockAsync(Block block)
        {
            var publicKey = await _accountService.GetPublicKeyAsync();
            block.Sign(publicKey, data => _accountService.SignAsync(data));
        }
    }
}