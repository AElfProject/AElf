using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
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
        private readonly ITxHub _txHub;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IAccountService _accountService;

        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;

        public ILocalEventBus EventBus { get; set; }

        private const float RatioMine = 0.3f;

        public MinerService(ITxHub txHub, IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockchainService blockchainService, IBlockExecutingService blockExecutingService,
            IConsensusService consensusService)
        {
            _txHub = txHub;
            Logger = NullLogger<MinerService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _consensusService = consensusService;
            _blockchainService = blockchainService;
            _accountService = accountService;
            
            EventBus = NullLocalEventBus.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<IBlock> MineAsync(int chainId, Hash previousBlockHash, ulong previousBlockHeight,
            DateTime time)
        {
            try
            {
                // generate block without txns
                var block = await GenerateBlock(chainId, previousBlockHash, previousBlockHeight);

                var transactions = await GenerateSystemTransactions(chainId, previousBlockHash, previousBlockHeight);

                var txInPool = (await _txHub.GetReceiptsOfExecutablesAsync()).Select(p => p.Transaction).ToList();

                using (var cts = new CancellationTokenSource())
                {
                    // Give 400 ms for packing
                    //cts.CancelAfter(time - DateTime.UtcNow - TimeSpan.FromMilliseconds(400));
                    block =
                        await _blockExecutingService.ExecuteBlockAsync(chainId, block.Header, transactions, txInPool,
                            cts.Token);
                }

                /*DateTime currentBlockTime = block.Header.Time.ToDateTime();
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

                var blockStateSet = new BlockStateSet()
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height,
                    PreviousHash = block.Header.PreviousBlockHash
                };
                FillBlockStateSet(blockStateSet, traces); 
                await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);*/

                await SignBlockAsync(block);
                await _blockchainService.AddBlockAsync(chainId, block);
                var chain = await _blockchainService.GetChainAsync(chainId);
                await _blockchainService.AttachBlockToChainAsync(chain, block);

                await SignBlockAsync(block);
                // TODO: TxHub needs to be updated when BestChain is found/extended, so maybe the call should be centralized
                //await _txHub.OnNewBlock(block);

                Logger.LogInformation($"Generate block {block.BlockHashToHex} at height {block.Header.Height} " +
                                      $"with {block.Body.TransactionsCount} txs.");

                return block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Mining failed with exception.");
                return null;
            }
        }

        private async Task<List<Transaction>> GenerateSystemTransactions(int chainId, Hash previousBlockHash,
            ulong previousBlockHeight)
        {
            var previousBlockPrefix = previousBlockHash.Value.Take(4).ToArray();
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());

            var generatedTxns =
                _systemTransactionGenerationService.GenerateSystemTransactions(address, previousBlockHeight,
                    previousBlockPrefix, chainId);

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

        private async Task SignBlockAsync(Block block)
        {
            var publicKey = await _accountService.GetPublicKeyAsync();
            block.Sign(publicKey, data => _accountService.SignAsync(data));
        }
    }
}