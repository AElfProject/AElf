using System.Threading.Tasks;
using AElf.Kernel;
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

        public BlockSyncValidationServiceTests()
        {
            _blockSyncValidationService = GetRequiredService<IBlockSyncValidationService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
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

            var validateResult = await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement);

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

            var validateResult = await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement);
            validateResult.ShouldBeTrue();
            
            validateResult = await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement);
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

            var validateResult = await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement);

            validateResult.ShouldBeFalse();
        }
    }
}