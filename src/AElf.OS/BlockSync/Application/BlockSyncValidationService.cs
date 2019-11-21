using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
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
        private readonly ITxHub _txHub;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        private readonly ITransactionValidationService _transactionValidationService;

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockValidationService blockValidationService, ITxHub txHub,
            ITransactionValidationService transactionValidationService)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockValidationService = blockValidationService;
            _txHub = txHub;
            _transactionValidationService = transactionValidationService;
        }

        public Task<bool> ValidateAnnouncementBeforeSyncAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubKey)
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
                if (!transaction.VerifyExpiration(blockWithTransactions.Height - 1))
                {
                    Logger.LogWarning($"Transaction {transaction.GetHash()} expired.");
                    return false;
                }

                // No need to validate again if this tx already in local database.
                if (await _txHub.IsTransactionExistsAsync(transaction.GetHash()))
                {
                    continue;
                }

                if (!await _transactionValidationService.ValidateTransactionAsync(transaction))
                {
                    return false;
                }

                var constrainedTransactionValidationResult =
                    _transactionValidationService.ValidateConstrainedTransaction(transaction,
                        blockWithTransactions.GetHash());
                _transactionValidationService.ClearConstrainedTransactionValidationProvider(blockWithTransactions
                    .GetHash());
                if (!constrainedTransactionValidationResult)
                {
                    Logger.LogWarning($"Transaction {transaction} validation failed for constraint.");
                    return false;
                }
            }

            return true;
        }
    }
}