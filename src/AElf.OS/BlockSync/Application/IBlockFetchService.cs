using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockFetchService
    {
        Task<bool> FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey);
    }

    public class BlockFetchService : IBlockFetchService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IBlockSyncAttachService _blockSyncAttachService;

        public ILogger<BlockFetchService> Logger { get; set; }

        public BlockFetchService(IBlockSyncAttachService blockSyncAttachService,
            IBlockchainService blockchainService,
            INetworkService networkService)
        {
            Logger = NullLogger<BlockFetchService>.Instance;

            _blockchainService = blockchainService;
            _networkService = networkService;
            _blockSyncAttachService = blockSyncAttachService;
        }

        public async Task<bool> FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey)
        {
            var localBlock = await _blockchainService.GetBlockByHashAsync(blockHash);
            if (localBlock != null)
            {
                Logger.LogDebug($"Block {localBlock} already know.");
                return true;
            }

            var blockWithTransactions = await _networkService.GetBlockByHashAsync(blockHash, suggestedPeerPubKey);

            if (blockWithTransactions == null)
            {
                return false;
            }

            _blockSyncAttachService.EnqueueAttachBlockWithTransactionsJob(blockWithTransactions);

            return true;
        }
    }
}