using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public sealed class BlockSyncServiceForkedTests : BlockSyncForkedTestBase
    {
        private readonly IBlockchainService _blockChainService;
        private readonly IBlockSyncService _blockSyncService;
        private readonly INetworkService _networkService;

        public BlockSyncServiceForkedTests()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _blockSyncService = GetRequiredService<IBlockSyncService>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task SyncBlock_FromLIB_Success()
        {
            var chain = await _blockChainService.GetChainAsync();
            var originalBestChainHash = chain.BestChainHash;
            var originalBestChainHeight = chain.BestChainHeight;
            var peerBlocks = await _networkService.GetBlocksAsync(chain.LastIrreversibleBlockHash, 30);

            var peerBlock = peerBlocks.Last();
            var peerBlockHash = peerBlock.GetHash();
            var peerBlockHeight = peerBlock.Header.Height;

            await _blockSyncService.SyncByAnnouncementAsync(chain, new SyncAnnouncementDto
            {
                SyncBlockHash = peerBlockHash,
                SyncBlockHeight = peerBlockHeight,
                BatchRequestBlockCount = 5
            });

            chain = await _blockChainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(30);
            chain.BestChainHash.ShouldBe(peerBlockHash);

            var block = await _blockChainService.GetBlockByHeightInBestChainBranchAsync(originalBestChainHeight);
            block.GetHash().ShouldNotBe(originalBestChainHash);
        }
    }
}