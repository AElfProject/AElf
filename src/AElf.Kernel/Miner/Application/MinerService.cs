using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;
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
        private readonly ITransactionPoolService _transactionPoolService;
        private readonly IMiningService _miningService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly EvilTriggerOptions _evilTriggerOptions;
        private readonly IBlockchainService _blockchainService;


        public MinerService(IMiningService miningService,
            IBlockTransactionLimitProvider blockTransactionLimitProvider,
            ITransactionPackingOptionProvider transactionPackingOptionProvider,
            ITransactionPoolService transactionPoolService, IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions,
            IBlockchainService blockchainService)
        {
            _miningService = miningService;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _transactionPackingOptionProvider = transactionPackingOptionProvider;
            _transactionPoolService = transactionPoolService;

            _evilTriggerOptions = evilTriggerOptions.CurrentValue;
            _blockchainService = blockchainService;
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
            var txList = new List<Transaction>();

            var chainContext = new ChainContext
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            };

            var limit = await _blockTransactionLimitProvider.GetLimitAsync(chainContext);
            if (_evilTriggerOptions.OverBlockTransactionLimit &&
                chainContext.BlockHeight % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                limit *= 2;
                Logger.LogWarning($"EVIL TRIGGER - Over block transaction limit: {limit}");
            }

            if (_transactionPackingOptionProvider.IsTransactionPackable(chainContext))
            {
                var executableTransactionSet = await _transactionPoolService.GetExecutableTransactionSetAsync(
                    previousBlockHash, limit);

                txList.AddRange(executableTransactionSet.Transactions);
            }

            if (_evilTriggerOptions.RepackagedTransaction &&
                (previousBlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                var block = await _blockchainService.GetBlockByHashAsync(previousBlockHash);
                if (block.TransactionIds.Count() > 5 && txList.Count > 0)
                {
                    var lastTxId = block.TransactionIds.Last();
                    var alreadyExecutedTransaction =
                        await _blockchainService.GetTransactionsAsync(new[] {lastTxId});
                    Logger.LogWarning($"EVIL TRIGGER - RepackagedTransaction  - Tx {lastTxId} ");
                    txList.RemoveAt(txList.Count - 1);
                    txList.AddRange(alreadyExecutedTransaction);
                }
            }

            Logger.LogInformation(
                $"Start mining with previous hash: {previousBlockHash}, previous height: {previousBlockHeight}.");
            return await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash,
                    PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = blockExecutionTime,
                    TransactionCountLimit = limit
                }, txList, blockTime);
        }
    }
}