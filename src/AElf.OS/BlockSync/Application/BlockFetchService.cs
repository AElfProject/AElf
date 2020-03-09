using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Events;
using AElf.OS.BlockSync.Exceptions;
using AElf.OS.Network.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.BlockSync.Application
{
    public class BlockFetchService : IBlockFetchService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;

        public ILogger<BlockFetchService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public BlockFetchService(IBlockSyncAttachService blockSyncAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockFetchService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockSyncAttachService = blockSyncAttachService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task<bool> FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey)
        {
            var hasBlock = await _blockchainService.HasBlockAsync(blockHash);
            if (hasBlock)
            {
                Logger.LogDebug($"Block {blockHash} already know.");
                return true;
            }

            var response = await _networkService.GetBlockByHashAsync(blockHash, suggestedPeerPubKey);

            if (!response.Success || response.Payload == null)
            {
                return false;
            }

            var blockWithTransactions = response.Payload;
            if (blockWithTransactions.GetHash() != blockHash || blockWithTransactions.Height != blockHeight)
            {
                Logger.LogWarning(
                    $"Fetched invalid block, peer: {suggestedPeerPubKey}, block hash: {blockWithTransactions.GetHash()}, block height: {blockWithTransactions.Height}");
                await LocalEventBus.PublishAsync(new AbnormalPeerFoundEventData
                {
                    BlockHash = blockWithTransactions.GetHash(),
                    BlockHeight = blockWithTransactions.Height,
                    PeerPubkey = suggestedPeerPubKey
                });

                return false;
            }

            _blockSyncQueueService.Enqueue(
                async () =>
                {
                    await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions,
                        suggestedPeerPubKey);
                },
                OSConstants.BlockSyncAttachQueueName);

            return true;
        }
    }
}