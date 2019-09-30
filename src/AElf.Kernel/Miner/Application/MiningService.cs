using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
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

        public ILocalEventBus EventBus { get; set; }

        public MiningService(IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockExecutingService blockExecutingService,
            IBlockchainService blockchainService)
        {
            Logger = NullLogger<MiningService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _accountService = accountService;
            _blockchainService = blockchainService;

            EventBus = NullLocalEventBus.Instance;
        }

        private async Task<List<Transaction>> GenerateSystemTransactions(Hash previousBlockHash,
            long previousBlockHeight)
        {
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());
            var systemTransactions = _systemTransactionGenerationService.GenerateSystemTransactions(address,
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

        public async Task<Block> MineAsync(RequestMiningDto requestMiningDto, List<Transaction> transactions,
            Timestamp blockTime)
        {
            using (var cts = new CancellationTokenSource())
            {
                var expirationTime = blockTime + requestMiningDto.BlockExecutionTime;
                
                if (expirationTime < TimestampHelper.GetUtcNow())
                    cts.Cancel();
                else
                {
                    var ts = (expirationTime - TimestampHelper.GetUtcNow()).ToTimeSpan();
                    if (ts.TotalMilliseconds > 4000)
                    {
                        ts = TimeSpan.FromMilliseconds(4000);
                    }
                    cts.CancelAfter(ts);
                }
                
                var block = await GenerateBlock(requestMiningDto.PreviousBlockHash,
                    requestMiningDto.PreviousBlockHeight, blockTime);
                var systemTransactions = await GenerateSystemTransactions(requestMiningDto.PreviousBlockHash,
                    requestMiningDto.PreviousBlockHeight);
                
                var pending = transactions;
                
                block = await _blockExecutingService.ExecuteBlockAsync(block.Header,
                    systemTransactions, pending, cts.Token);
                await SignBlockAsync(block);
                Logger.LogInformation($"Generated block: {block.ToDiagnosticString()}, " +
                                      $"previous: {block.Header.PreviousBlockHash}, " +
                                      $"executed transactions: {block.Body.TransactionsCount}, " +
                                      $"not executed transactions {transactions.Count + systemTransactions.Count - block.Body.TransactionsCount}");
                return block;
            }
        }
    }
}