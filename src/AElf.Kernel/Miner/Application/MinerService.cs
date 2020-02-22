using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Txn.Application;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Miner.Application
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger { get; set; }
        private readonly ITxHub _txHub;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly ITransactionPackingService _transactionPackingService;
        private readonly IMiningService _miningService;

        public MinerService(IMiningService miningService, ITxHub txHub,
            IBlockTransactionLimitProvider blockTransactionLimitProvider,
            ITransactionPackingService transactionPackingService)
        {
            _miningService = miningService;
            _txHub = txHub;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _transactionPackingService = transactionPackingService;

            Logger = NullLogger<MinerService>.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            Duration blockExecutionTime)
        {
            Logger.LogInformation("### Get executable transactions start.");
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(new ChainContext
                {BlockHash = previousBlockHash, BlockHeight = previousBlockHeight});
            var executableTransactionSet =
                await _txHub.GetExecutableTransactionSetAsync(_transactionPackingService.IsTransactionPackingEnabled()
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
            Logger.LogInformation("### Get executable transactions end.");
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