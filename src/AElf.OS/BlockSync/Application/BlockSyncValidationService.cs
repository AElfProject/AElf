using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
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
        private readonly ITransactionManager _transactionManager;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        private readonly ITransactionValidationService _transactionValidationService;

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockValidationService blockValidationService, ITransactionManager transactionManager,
            ITransactionValidationService transactionValidationService)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockValidationService = blockValidationService;
            _transactionManager = transactionManager;
            _transactionValidationService = transactionValidationService;
        }

        public Task<bool> ValidateAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubKey)
        {
            if (!TryCacheNewAnnouncement(blockAnnouncement.BlockHash, blockAnnouncement.BlockHeight, senderPubKey))
            {
                return Task.FromResult(false);
            }

            if (blockAnnouncement.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockAnnouncement.BlockHash}, height: {blockAnnouncement.BlockHeight} }} ignore.");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool> ValidateBlockBeforeSyncAsync(Chain chain, BlockWithTransactions blockWithTransactions, string senderPubKey)
        {
            if (blockWithTransactions.Height <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning($"Receive lower block {blockWithTransactions} ignore.");
                return Task.FromResult(false);
            }

            if (blockWithTransactions.Header.SignerPubkey.ToHex() != senderPubKey)
            {
                Logger.LogWarning($"Sender {senderPubKey} of block {blockWithTransactions} is incorrect.");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        
        public async Task<bool> ValidateBlockBeforeAttachAsync(BlockWithTransactions blockWithTransactions)
        {
            if (!await _blockValidationService.ValidateBlockBeforeAttachAsync(blockWithTransactions))
            {
                return false;
            }

            if (!await ValidateTransactionAsync(blockWithTransactions))
            {
                return false;
            }

            return true;
        }
        
        private bool TryCacheNewAnnouncement(Hash blockHash, long blockHeight, string senderPubkey)
        {
            return _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight, senderPubkey);
        }

        private async Task<bool> ValidateTransactionAsync(BlockWithTransactions blockWithTransactions)
        {
            foreach (var transaction in blockWithTransactions.Transactions)
            {
                // No need to validate again if this tx already in local database.
                var tx = await _transactionManager.GetTransactionAsync(transaction.GetHash());
                if (tx != null)
                    continue;

                if (!await _transactionValidationService.ValidateTransactionAsync(transaction))
                {
                    return false;
                }

                if (!_transactionValidationService.ValidateConstrainedTransaction(transaction,
                    blockWithTransactions.GetHash()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}