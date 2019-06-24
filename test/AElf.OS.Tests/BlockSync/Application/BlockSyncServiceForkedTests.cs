using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public sealed class BlockSyncServiceForkedTests : BlockSyncForkedTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly IBlockSyncService _blockSyncService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly INetworkService _networkService;

        public BlockSyncServiceForkedTests()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _blockSyncService = GetRequiredService<IBlockSyncService>();
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task SyncBlock_FromLIB_Success()
        {
            var chain = await _blockChainService.GetChainAsync();
            var originalBestChainHash = chain.BestChainHash;
            var originalBestChainHeight = chain.BestChainHeight;
            var peerBlocks = await _networkService.GetBlocksAsync(chain.LastIrreversibleBlockHash, 20);

            var peerBlock = peerBlocks.Last();
            var peerBlockHash = peerBlock.GetHash();
            var peerBlockHeight = peerBlock.Header.Height;
            
            await _blockSyncService.SyncBlockAsync(peerBlockHash, peerBlockHeight, 5, null);
            
            chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(20);
            chain.BestChainHash.ShouldBe(peerBlockHash);

            var block = await _blockChainService.GetBlockByHeightInBestChainBranchAsync(originalBestChainHeight);
            block.GetHash().ShouldNotBe(originalBestChainHash);
        }
        
    }
}