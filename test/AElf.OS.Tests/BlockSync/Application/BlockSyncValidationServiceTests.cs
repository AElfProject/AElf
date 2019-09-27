using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockchainService _blockchainService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IAccountService _accountService;
        private readonly OSTestHelper _osTestHelper;

        public BlockSyncValidationServiceTests()
        {
            _blockSyncValidationService = GetRequiredService<IBlockSyncValidationService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
            _accountService = GetRequiredService<IAccountService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task ValidateAnnouncement_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var blockAnnouncement = new BlockAnnouncement
            {
                BlockHash = Hash.FromString("SyncBlockHash"),
                BlockHeight = chain.LastIrreversibleBlockHeight + 1
            };

            var validateResult =
                await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement,
                    GetEncodedPubKeyString());

            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateAnnouncement_AlreadySynchronized()
        {
            var chain = await _blockchainService.GetChainAsync();

            var blockAnnouncement = new BlockAnnouncement
            {
                BlockHash = Hash.FromString("SyncBlockHash"),
                BlockHeight = chain.LastIrreversibleBlockHeight + 1
            };

            var validateResult =
                await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement,
                    GetEncodedPubKeyString());
            validateResult.ShouldBeTrue();

            validateResult =
                await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement,
                    GetEncodedPubKeyString());
            validateResult.ShouldBeFalse();
        }

        [Fact]
        public async Task ValidateAnnouncement_LessThenLIBHeight()
        {
            var chain = await _blockchainService.GetChainAsync();

            var blockAnnouncement = new BlockAnnouncement
            {
                BlockHash = Hash.FromString("SyncBlockHash"),
                BlockHeight = chain.LastIrreversibleBlockHeight
            };

            var validateResult =
                await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement,
                    GetEncodedPubKeyString());

            validateResult.ShouldBeFalse();
        }

        [Fact]
        public async Task ValidateBlock_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var block = _osTestHelper.GenerateBlockWithTransactions(chain.LastIrreversibleBlockHash,
                chain.LastIrreversibleBlockHeight);
            var pubkey = (await _accountService.GetPublicKeyAsync()).ToHex();

            var validateResult = await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, pubkey);

            validateResult.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ValidateBlock_LessThenLIBHeight()
        {
            var chain = await _blockchainService.GetChainAsync();

            var block = _osTestHelper.GenerateBlockWithTransactions(Hash.FromString("SyncBlockHash"),
                chain.LastIrreversibleBlockHeight - 1);
            var pubkey = (await _accountService.GetPublicKeyAsync()).ToHex();

            var validateResult = await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, pubkey);

            validateResult.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateBlock_IncorrectSender()
        {
            var chain = await _blockchainService.GetChainAsync();

            var block = _osTestHelper.GenerateBlockWithTransactions(Hash.FromString("SyncBlockHash"),
                chain.LastIrreversibleBlockHeight - 1);

            var validateResult = await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, GetEncodedPubKeyString());

            validateResult.ShouldBeFalse();
        }

        private string GetEncodedPubKeyString()
        {
            var pk = CryptoHelper.GenerateKeyPair().PublicKey;
            var address = Address.FromPublicKey(pk);
            return address.GetFormatted();
        }

        [Fact]
        public void TryAddAnnouncementCache_MultipleTimes()
        {
            for (var i = 0; i < 120; i++)
            {
                var blockHash = Hash.FromString(Guid.NewGuid().ToString());
                var blockHeight = i;
                var result = _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight, GetEncodedPubKeyString());
                result.ShouldBeTrue();
            }
        }
    }
}