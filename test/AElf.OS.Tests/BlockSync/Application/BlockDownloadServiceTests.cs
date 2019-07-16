using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceTests : BlockSyncTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockchainService _blockchainService;

        public BlockDownloadServiceTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task DownloadBlocks_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash, chain
                .BestChainHeight, 5, null);

            downloadResult.ShouldBe(20);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(31);
        }

        [Fact]
        public async Task DownloadBlocks_MoreThanLimit()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(Hash.FromString("MoreThanLimit"), 62, 5, null);

            downloadResult.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
        
        [Fact]
        public async Task DownloadBlocks_NoBlockReturn()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(Hash.FromString("NoBlockReturn"), 15, 5, null);

            downloadResult.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
    }
}