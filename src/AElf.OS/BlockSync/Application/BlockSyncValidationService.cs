using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationService : IBlockSyncValidationService
    {
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IBlockValidationService _blockValidationService;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        private IEnumerable<IBlockSyncTransactionValidationProvider> _blockSyncTransactionValidationProviders;

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockValidationService blockValidationService, IEnumerable<IBlockSyncTransactionValidationProvider> blockSyncTransactionValidationProviders)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockValidationService = blockValidationService;
            _blockSyncTransactionValidationProviders = blockSyncTransactionValidationProviders;
        }

        public async Task<bool> ValidateAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement)
        {
            if (!_announcementCacheProvider.TryAddAnnouncementCache(blockAnnouncement.BlockHash,
                blockAnnouncement.BlockHeight))
            {
                return false;
            }

            if (blockAnnouncement.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockAnnouncement.BlockHash}, height: {blockAnnouncement.BlockHeight} }} ignore.");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions)
        {
            if (!_announcementCacheProvider.TryAddAnnouncementCache(blockWithTransactions.GetHash(),
                blockWithTransactions.Height))
            {
                return false;
            }

            if (blockWithTransactions.Height <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning($"Receive lower block {blockWithTransactions} ignore.");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateTransactionAsync(IEnumerable<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                foreach (var validationProvider in _blockSyncTransactionValidationProviders)
                {
                    if (!await validationProvider.ValidateTransactionAsync(transaction))
                        return false;
                }
            }

            return true;
        }

        public async Task<bool> ValidateBlockBeforeAttachAsync(BlockWithTransactions blockWithTransactions)
        {
            return await _blockValidationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
        }
    }
}