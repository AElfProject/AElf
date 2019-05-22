using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockFetchService
    {
        Task FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey);
    }

    public class BlockFetchService : IBlockFetchService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockValidationService _validationService;

        public ILogger<BlockFetchService> Logger { get; set; }

        public BlockFetchService(IBlockAttachService blockAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager,
            IBlockSyncStateProvider blockSyncStateProvider,
            IBlockValidationService validationService)
        {
            Logger = NullLogger<BlockFetchService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockSyncStateProvider = blockSyncStateProvider;
            _validationService = validationService;
        }

        public async Task FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey)
        {
            var localBlock = await _blockchainService.GetBlockByHashAsync(blockHash);
            if (localBlock != null)
            {
                Logger.LogDebug($"Block {localBlock} already know.");
                return;
            }

            var blockWithTransactions = await _networkService.GetBlockByHashAsync(blockHash, suggestedPeerPubKey);
            
            var valid = await _validationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"The block was invalid, block hash: {blockWithTransactions} , sync from {suggestedPeerPubKey} failed.");
            }
            
            await _blockchainService.AddTransactionsAsync(blockWithTransactions.Transactions);
            var block = blockWithTransactions.ToBlock();
            await _blockchainService.AddBlockAsync(block);

            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncJobEnqueueTime = enqueueTimestamp;
                        await _blockAttachService.AttachBlockAsync(block);
                    }
                    finally
                    {
                        _blockSyncStateProvider.BlockSyncJobEnqueueTime = null;
                    }
                },
                KernelConstants.UpdateChainQueueName);
        }
    }
}