using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Options;
using NuGet.Frameworks;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceTests : BlockSyncTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockchainService _blockchainService;
        private readonly BlockSyncOptions _blockSyncOptions;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public BlockDownloadServiceTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockSyncOptions = GetRequiredService<IOptionsSnapshot<BlockSyncOptions>>().Value;
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
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
        public async Task DownloadBlocks_UseSuggestedPeer()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                UseSuggestedPeer = true
            });

            downloadResult.DownloadBlockCount.ShouldBe(20);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(31);
        }
        
        [Fact]
        public async Task DownloadBlocks_NetworkException()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                SuggestedPeerPubkey = "NetworkException",
                UseSuggestedPeer = true
            });

            downloadResult.Success.ShouldBeFalse();
            downloadResult.DownloadBlockCount.ShouldBe(0);
        }

        [Fact]
        public async Task DownloadBlocks_MoreThanLimit()
        {
            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("MoreThanLimit"),
                PreviousBlockHeight = 62,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
            });

            downloadResult.DownloadBlockCount.ShouldBe(0);
            
            var chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
        
        [Fact]
        public async Task DownloadBlocks_NoBlockReturn()
        {
            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("NoBlockReturn"),
                PreviousBlockHeight = 15,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
            });

            downloadResult.DownloadBlockCount.ShouldBe(0);
            
            var chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }
        
        [Fact]
        public async Task DownloadBlocks_RemovedPeer()
        {
            var chain = await _blockchainService.GetChainAsync();

            var downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
                MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                SuggestedPeerPubkey = "RemovedPeer",
                UseSuggestedPeer = true
            });

            downloadResult.Success.ShouldBeFalse();
            downloadResult.DownloadBlockCount.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(11);
        }

        [Fact]
        public void ValidateQueueAvailabilityBeforeDownload_Test()
        {
            _blockDownloadService.ValidateQueueAvailabilityBeforeDownload().ShouldBeTrue();
            
            _blockSyncStateProvider.SetEnqueueTime(OSConstants.BlockSyncAttachQueueName,
                TimestampHelper.GetUtcNow()
                    .AddMilliseconds(-BlockSyncConstants.BlockSyncAttachBlockAgeLimit - 1000));
            _blockDownloadService.ValidateQueueAvailabilityBeforeDownload().ShouldBeFalse();
            
            _blockSyncStateProvider.SetEnqueueTime(KernelConstants.UpdateChainQueueName,
                TimestampHelper.GetUtcNow()
                    .AddMilliseconds(-BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit - 1000));
            _blockDownloadService.ValidateQueueAvailabilityBeforeDownload().ShouldBeFalse();
        }

        [Fact]
        public void RemoveDownloadJobTargetState_Test()
        {
            var targetHash = HashHelper.ComputeFrom("TargetHash");
            _blockSyncStateProvider.SetDownloadJobTargetState(targetHash, true);
            
            _blockDownloadService.RemoveDownloadJobTargetState(null);
            _blockSyncStateProvider.TryGetDownloadJobTargetState(targetHash, out _).ShouldBeTrue();
            
            _blockDownloadService.RemoveDownloadJobTargetState(targetHash);
            _blockSyncStateProvider.TryGetDownloadJobTargetState(targetHash, out _).ShouldBeFalse();
        }

        [Fact]
        public void IsNotReachedDownloadTarget_Test()
        {
            var targetHash = HashHelper.ComputeFrom("TargetHash");
            _blockDownloadService.IsNotReachedDownloadTarget(targetHash).ShouldBeFalse();
            
            _blockSyncStateProvider.SetDownloadJobTargetState(targetHash, false);
            _blockDownloadService.IsNotReachedDownloadTarget(targetHash).ShouldBeTrue();
            
            _blockSyncStateProvider.SetDownloadJobTargetState(targetHash, true);
            _blockDownloadService.IsNotReachedDownloadTarget(targetHash).ShouldBeFalse();
        }
    }
}