using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Txn.Application;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Miner.Application
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger { get; set; }
        private readonly ITxHub _txHub;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        private readonly EvilTriggerOptions _evilTriggerOptions;
        private readonly IMiningService _miningService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;

        public MinerService(IMiningService miningService, ITxHub txHub,
            IBlockTransactionLimitProvider blockTransactionLimitProvider,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions, IBlockchainService blockchainService)
        {
            _miningService = miningService;
            _txHub = txHub;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _blockchainService = blockchainService;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
            _evilTriggerOptions = evilTriggerOptions.CurrentValue;
            Logger = NullLogger<MinerService>.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<BlockExecutedSet> MineAsync(Hash previousBlockHash, long previousBlockHeight,
            Timestamp blockTime,
            Duration blockExecutionTime)
        {
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(new ChainContext
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            });
            var executableTransactionSet =
                await _txHub.GetExecutableTransactionSetAsync(_transactionPackingOptions.IsTransactionPackable
                    ? limit
                    : -1);

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

            if (_evilTriggerOptions.RepeatTransactionInOneBlockAttack &&
                (previousBlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber == 0 && pending.Any())
            {
                var last = pending.Last();
                Logger.LogWarning($"EVIL TRIGGER - RepeatTransactionInOneBlockAttack!!! - Tx {last.GetHash()}");
                pending.Add(last);
            }
            
            if (_evilTriggerOptions.DoubleSpendAttack &&
                (previousBlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                var block = await _blockchainService.GetBlockByHashAsync(previousBlockHash);
                if (block.TransactionIds.Count() > 5)
                {
                    var lastTxId = block.TransactionIds.Last();
                    var alreadyExecutedTransaction =
                        await _blockchainService.GetTransactionsAsync(new[] {lastTxId});
                    Logger.LogWarning($"EVIL TRIGGER - DoubleSpendAttack!!! - Tx {lastTxId} ");
                    pending.AddRange(alreadyExecutedTransaction);
                }
            }


            Logger.LogDebug(
                $"Start mining with previous hash: {previousBlockHash}, previous height: {previousBlockHeight}.");
            return await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash,
                    PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = blockExecutionTime
                }, pending, blockTime);
        }
    }
}