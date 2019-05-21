using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using Google.Protobuf.WellKnownTypes;
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

        public ILogger<BlockFetchService> Logger { get; set; }

        public BlockFetchService(IBlockAttachService blockAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService,
            ITaskQueueManager taskQueueManager,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockFetchService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey)
        {
            var peerBlock = await _blockchainService.GetBlockByHashAsync(blockHash);
            if (peerBlock != null)
            {
                Logger.LogDebug($"Block {peerBlock} already know.");
                return;
            }

            peerBlock = await _networkService.GetBlockByHashAsync(blockHash, suggestedPeerPubKey);
            if (peerBlock == null)
            {
                Logger.LogWarning($"Get null block from peer, request block hash: {blockHash}");
                return;
            }

            var enqueueTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncJobEnqueueTime = enqueueTimestamp;
                        await _blockAttachService.AttachBlockAsync(peerBlock);
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