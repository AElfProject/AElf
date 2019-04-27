using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
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
        private IMiningService _miningService;
        public ILocalEventBus EventBus { get; set; }

        private const float RatioMine = 0.3f;

        public MinerService(IMiningService miningService, ITxHub txHub)
        {
            _miningService = miningService;
            _txHub = txHub;

            EventBus = NullLocalEventBus.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, DateTime dateTime,
            TimeSpan timeSpan)
        {
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

            return await _miningService.MineAsync(previousBlockHash, previousBlockHeight, pending, dateTime, timeSpan);
        }
    }


    public class MiningService : IMiningService
    {
        public ILogger<MiningService> Logger { get; set; }
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockExecutingService _blockExecutingService;
        public ILocalEventBus EventBus { get; set; }

        public MiningService(IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockExecutingService blockExecutingService)
        {
            Logger = NullLogger<MiningService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _accountService = accountService;
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

            return systemTransactions;
        }

        private async Task SignAsync(Transaction notSignerTransaction)
        {
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().DumpByteArray());
            notSignerTransaction.Signature = ByteString.CopyFrom(signature);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <returns></returns>
        private async Task<Block> GenerateBlock(Hash preBlockHash, long preBlockHeight, DateTime expectedMiningTime)
        {
            var block = await _blockGenerationService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
            {
                PreviousBlockHash = preBlockHash,
                PreviousBlockHeight = preBlockHeight,
                BlockTime = expectedMiningTime
            });
            return block;
        }

        private async Task SignBlockAsync(Block block)
        {
            block.Header.SignerKey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync());
            var signature = await _accountService.SignAsync(block.GetHash().DumpByteArray());
            block.Header.Signature = ByteString.CopyFrom(signature);
        }

        public async Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight,
            List<Transaction> transactions, DateTime blockTime, TimeSpan timeSpan)
        {
            var block = await GenerateBlock(previousBlockHash, previousBlockHeight, blockTime);
            var systemTransactions = await GenerateSystemTransactions(previousBlockHash, previousBlockHeight);

            var pending = transactions;

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(timeSpan);
                block = await _blockExecutingService.ExecuteBlockAsync(block.Header,
                    systemTransactions, pending, cts.Token);
            }

            await SignBlockAsync(block);
            // TODO: TxHub needs to be updated when BestChain is found/extended, so maybe the call should be centralized
            //await _txHub.OnNewBlock(block);

            Logger.LogInformation($"Generated block: {block.ToDiagnosticString()}, " +
                                  $"previous: {block.Header.PreviousBlockHash}, " +
                                  $"transactions: {block.Body.TransactionsCount}");

            return block;
        }
    }
}