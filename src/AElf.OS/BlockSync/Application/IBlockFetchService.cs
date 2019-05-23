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
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<BlockFetchService> Logger { get; set; }

        public BlockFetchService(IBlockSyncAttachService blockSyncAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager)
        {
            Logger = NullLogger<BlockFetchService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockSyncAttachService = blockSyncAttachService;
            _taskQueueManager = taskQueueManager;
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

            _taskQueueManager.Enqueue(async () => await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions),
                OSConsts.BlockSyncAttachQueueName);
        }
    }
}