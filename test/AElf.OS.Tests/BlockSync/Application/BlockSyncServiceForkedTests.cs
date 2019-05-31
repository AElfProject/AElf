using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public sealed class BlockSyncServiceForkedTests : BlockSyncForkedTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly IBlockSyncService _blockSyncService;
        private readonly BlockSyncTestHelper _blockSyncTestHelper;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;

        public BlockSyncServiceForkedTests()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _blockSyncService = GetRequiredService<IBlockSyncService>();
            _blockSyncTestHelper = GetRequiredService<BlockSyncTestHelper>();
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
        }

        [Fact]
        public async Task SyncBlock_FromLIB_Success()
        {
            var chain = await _blockChainService.GetChainAsync();
            var peerBlockHash = Hash.FromString("PeerBlockHash");
            var peerBlockHeight = chain.BestChainHeight + 8;
            
            await _blockSyncService.SyncBlockAsync(peerBlockHash, peerBlockHeight, 5, null);

            _blockSyncTestHelper.DisposeQueue();

            chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(20);
            
            _announcementCacheProvider.ContainsAnnouncement(peerBlockHash,peerBlockHeight).ShouldBeTrue();
        }
        
    }
}