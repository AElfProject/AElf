using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
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
        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;
        private readonly IMiningService _miningService;

        public MinerService(IMiningService miningService, ITxHub txHub,
            IBlockTransactionLimitProvider blockTransactionLimitProvider,
            ITransactionInclusivenessProvider transactionInclusivenessProvider)
        {
            _miningService = miningService;
            _txHub = txHub;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;

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
            var limit = _blockTransactionLimitProvider.GetLimit();
            var executableTransactionSet =
                await _txHub.GetExecutableTransactionSetAsync(_transactionInclusivenessProvider.IsTransactionPackable
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

            Logger.LogTrace(
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