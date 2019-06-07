using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        
        public BlockSyncServiceTests()
        {
            _blockSyncService = GetRequiredService<IBlockSyncService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
        }

        [Fact]
        public async Task SyncBlock_LessThenFetchLimit_Success()
        {
            var peerBlock = await _networkService.GetBlockByHashAsync(Hash.FromString("PeerBlock"));

            var block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.ShouldBeNull();

            await _blockSyncService.SyncBlockAsync(peerBlock.GetHash(), peerBlock.Height, 5, null);

            block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.GetHash().ShouldBe(peerBlock.GetHash());

            var chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(peerBlock.GetHash());
            chain.BestChainHeight.ShouldBe(peerBlock.Height);
             
            _announcementCacheProvider.ContainsAnnouncement(peerBlock.GetHash(),peerBlock.Height).ShouldBeTrue();
        }
        
        [Fact]
        public async Task SyncBlock_LessThenFetchLimit_FetchReturnFalse()
        {
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHash = chain.BestChainHash;
            var bestChainHeight = chain.BestChainHeight;
            
            await _blockSyncService.SyncBlockAsync(Hash.Empty, 15, 5, null);

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
             
            _announcementCacheProvider.ContainsAnnouncement(Hash.Empty,15).ShouldBeFalse();
        }

        [Fact]
        public async Task SyncBlock_MoreThenFetchLimit_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var peerBlockHash = Hash.FromString("PeerBlock");
            var peerBlockHeight = chain.BestChainHeight + 8;

            await _blockSyncService.SyncBlockAsync(peerBlockHash, peerBlockHeight, 5, null);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(21);

            _announcementCacheProvider.ContainsAnnouncement(peerBlockHash, peerBlockHeight).ShouldBeTrue();
        }
        
        [Fact]
        public async Task SyncBlock_QueueIsBusy()
        {
            _blockSyncStateProvider.BlockSyncJobEnqueueTime = TimestampHelper.GetUtcNow().AddMilliseconds(-600);
            
            var peerBlock = await _networkService.GetBlockByHashAsync(Hash.FromString("PeerBlock"));

            await _blockSyncService.SyncBlockAsync(peerBlock.GetHash(), peerBlock.Height, 5, null);
            
            var block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.ShouldBeNull();

            _announcementCacheProvider.ContainsAnnouncement(peerBlock.GetHash(),peerBlock.Height).ShouldBeFalse();
        }
        
        [Fact]
        public async Task SyncBlock_AlreadySynchronized()
        {
            var peerBlock = await _networkService.GetBlockByHashAsync(Hash.FromString("PeerBlock"));
            _announcementCacheProvider.CacheAnnouncement(peerBlock.GetHash(), peerBlock.Height);

            await _blockSyncService.SyncBlockAsync(peerBlock.GetHash(), peerBlock.Height, 5, null);

            var block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.ShouldBeNull();
        }
    }
}