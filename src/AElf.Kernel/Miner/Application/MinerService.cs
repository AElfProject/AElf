using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
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
        private readonly ITransactionPackingService _transactionPackingService;
        private readonly IMiningService _miningService;
        private readonly IBlockchainStateService _blockchainStateService;

        public MinerService(IMiningService miningService, ITxHub txHub,
            ITransactionPackingService transactionPackingService, 
            IBlockchainStateService blockchainStateService)
        {
            _miningService = miningService;
            _txHub = txHub;
            _transactionPackingService = transactionPackingService;
            _blockchainStateService = blockchainStateService;

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
            var limit = await _blockchainStateService.GetBlockExecutedDataAsync<BlockTransactionLimit>(new ChainContext
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            });
            var executableTransactionSet =
                await _txHub.GetExecutableTransactionSetAsync(_transactionPackingService.IsTransactionPackingEnabled()
                    ? limit?.Value ?? 0
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