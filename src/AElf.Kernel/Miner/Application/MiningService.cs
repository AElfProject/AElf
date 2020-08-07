using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Miner.Application
{
    public class MiningService : IMiningService
    {
        public ILogger<MiningService> Logger { get; set; }
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        public ILocalEventBus EventBus { get; set; }

        public MiningService(IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockExecutingService blockExecutingService,
            IBlockchainService blockchainService, 
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider)
        {
            Logger = NullLogger<MiningService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _accountService = accountService;
            _blockchainService = blockchainService;
            _systemTransactionExtraDataProvider = systemTransactionExtraDataProvider;

            EventBus = NullLocalEventBus.Instance;
        }

        private async Task<List<Transaction>> GenerateSystemTransactions(Hash previousBlockHash,
            long previousBlockHeight)
        {
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());
            var systemTransactions = await _systemTransactionGenerationService.GenerateSystemTransactionsAsync(address,
                previousBlockHeight, previousBlockHash);

            foreach (var transaction in systemTransactions)
            {
                await SignAsync(transaction);
            }

            await _blockchainService.AddTransactionsAsync(systemTransactions);

            return systemTransactions;
        }

        private async Task SignAsync(Transaction notSignerTransaction)
        {
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().ToByteArray());
            notSignerTransaction.Signature = ByteString.CopyFrom(signature);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <returns></returns>
        private async Task<Block> GenerateBlock(Hash preBlockHash, long preBlockHeight, Timestamp expectedMiningTime)
        {
            var block = await _blockGenerationService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
            {
                PreviousBlockHash = preBlockHash,
                PreviousBlockHeight = preBlockHeight,
                BlockTime = expectedMiningTime
            });
            block.Header.SignerPubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync());
            return block;
        }

        private async Task SignBlockAsync(Block block)
        {
            var signature = await _accountService.SignAsync(block.GetHash().ToByteArray());
            block.Header.Signature = ByteString.CopyFrom(signature);
        }

        public async Task<BlockExecutedSet> MineAsync(RequestMiningDto requestMiningDto, List<Transaction> transactions,
            Timestamp blockTime)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    var expirationTime = blockTime + requestMiningDto.BlockExecutionTime;
                    if (expirationTime < TimestampHelper.GetUtcNow())
                        cts.Cancel();
                    else
                    {
                        var ts = (expirationTime - TimestampHelper.GetUtcNow()).ToTimeSpan();
                        if (ts.TotalMilliseconds > int.MaxValue)
                        {
                            ts = TimeSpan.FromMilliseconds(int.MaxValue);
                        }

                        cts.CancelAfter(ts);
                    }

                    var block = await GenerateBlock(requestMiningDto.PreviousBlockHash,
                        requestMiningDto.PreviousBlockHeight, blockTime);
                    var systemTransactions = await GenerateSystemTransactions(requestMiningDto.PreviousBlockHash,
                        requestMiningDto.PreviousBlockHeight);
                    _systemTransactionExtraDataProvider.SetSystemTransactionCount(systemTransactions.Count,
                        block.Header);
                    var txTotalCount = transactions.Count + systemTransactions.Count;

                    var pending = txTotalCount > requestMiningDto.TransactionCountLimit
                        ? transactions
                            .Take(requestMiningDto.TransactionCountLimit - systemTransactions.Count)
                            .ToList()
                        : transactions;
                    var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header,
                        systemTransactions, pending, cts.Token);

                    block = blockExecutedSet.Block;
                    await SignBlockAsync(block);
                    Logger.LogInformation($"Generated block: {block.ToDiagnosticString()}, " +
                                          $"previous: {block.Header.PreviousBlockHash}, " +
                                          $"executed transactions: {block.Body.TransactionsCount}, " +
                                          $"not executed transactions {pending.Count + systemTransactions.Count - block.Body.TransactionsCount} ");
                    return blockExecutedSet;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed while mining block.");
                throw;
            }
        }
    }
}