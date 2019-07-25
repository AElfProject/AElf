using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.Types;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceTests : BlockSyncTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockchainService _blockchainService;
        private readonly BlockSyncOptions _blockSyncOptions;

        public BlockDownloadServiceTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockSyncOptions = GetRequiredService<IOptionsSnapshot<BlockSyncOptions>>().Value;
        }

        [Fact]
        public async Task DownloadBlocks_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
            });

            downloadResult.DownloadBlockCount.ShouldBe(20);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(31);
        }

        [Fact]
        public async Task DownloadBlocks_MoreThanLimit()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = Hash.FromString("MoreThanLimit"),
                PreviousBlockHeight = 62,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
            });

            downloadResult.DownloadBlockCount.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
        
        [Fact]
        public async Task DownloadBlocks_NoBlockReturn()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = Hash.FromString("NoBlockReturn"),
                PreviousBlockHeight = 15,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
            });

            downloadResult.DownloadBlockCount.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
    }
}