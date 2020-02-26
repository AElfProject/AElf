using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
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
        private readonly IBlockchainService _blockchainService;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        private readonly ITransactionValidationService _transactionValidationService;

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockValidationService blockValidationService,
            ITransactionValidationService transactionValidationService,
            IBlockchainService blockchainService)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockValidationService = blockValidationService;
            _transactionValidationService = transactionValidationService;
            _blockchainService = blockchainService;
        }

        public Task<bool> ValidateAnnouncementBeforeSyncAsync(Chain chain, BlockAnnouncement blockAnnouncement,
            string senderPubKey)
        {
            if (blockAnnouncement.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockAnnouncement.BlockHash}, height: {blockAnnouncement.BlockHeight} }} ignore.");
                return Task.FromResult(false);
            }

            if (!TryCacheNewAnnouncement(blockAnnouncement.BlockHash, blockAnnouncement.BlockHeight, senderPubKey))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool> ValidateBlockBeforeSyncAsync(Chain chain, BlockWithTransactions blockWithTransactions,
            string senderPubKey)
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
                if (await _blockchainService.HasTransactionAsync(transaction.GetHash()))
                {
                    continue;
                }

                if (!await _transactionValidationService.ValidateTransactionWhileSyncingAsync(transaction))
                {
                    return false;
                }

                var constrainedTransactionValidationResult =
                    _transactionValidationService.ValidateConstrainedTransaction(transaction,
                        blockWithTransactions.GetHash());
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