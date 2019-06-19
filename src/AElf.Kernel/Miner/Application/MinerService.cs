using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.TransactionPool.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Miner.Application
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger { get; set; }
        private ITxHub _txHub;
        private IMiningService _miningService;
        public ILocalEventBus EventBus { get; set; }

        public MinerService(IMiningService miningService, ITxHub txHub)
        {
            _miningService = miningService;
            _txHub = txHub;

            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<MinerService>.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, Timestamp dateTime,
            Duration blockExecutionTime)
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

            return await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash, PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = blockExecutionTime
                }, pending, dateTime);
        }
    }
}