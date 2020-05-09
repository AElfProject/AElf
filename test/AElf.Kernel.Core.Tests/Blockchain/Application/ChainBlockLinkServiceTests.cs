using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class ChainBlockLinkServiceTests: AElfKernelWithChainTestBase
    {
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IChainManager _chainManager;

        public ChainBlockLinkServiceTests()
        {
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _chainBlockLinkService = GetRequiredService<IChainBlockLinkService>();
            _chainManager = GetRequiredService<IChainManager>();
            
        }

        [Fact]
        public void GetCachedChainBlockLinks_Tests()
        {
            var bestCount = _kernelTestHelper.BestBranchBlockList.Count;
            var longestCount = _kernelTestHelper.LongestBranchBlockList.Count;
            var forkCount = _kernelTestHelper.ForkBranchBlockList.Count;
            var unlinkedCount = _kernelTestHelper.UnlinkedBranchBlockList.Count;
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks();
            chainBlockLinks.Count.ShouldBe(bestCount + longestCount + forkCount + unlinkedCount);
        }

        [Fact]
        public async Task GetNotExecutedChainBlockLinksAsync_Tests()
        { 
            var chain = await _chainManager.GetAsync();
            var blockHash = chain.BestChainHash;
            var count = 0;
            while (true)
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(blockHash);
                if (chainBlockLink != null)
                {
                    if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionNone)
                    {
                        count++;
                        if (chainBlockLink.PreviousBlockHash != null)
                            blockHash = chainBlockLink.PreviousBlockHash;
                        continue;
                    }
                    else if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionFailed)
                    {
                        count = 0;
                    }
                }

                break;
            }

            var chainBlockLinks = await _chainBlockLinkService.GetNotExecutedChainBlockLinksAsync(chain.BestChainHash);
            chainBlockLinks.Count.ShouldBe(count);
        }

        [Fact]
        public void CleanCachedChainBlockLinks_Tests()
        {
            _chainBlockLinkService.CleanCachedChainBlockLinks(long.MaxValue);
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks();
            chainBlockLinks.Count.ShouldBe(0);
        }

        [Fact]
        public void SetChainBlockLinkExecutionStatusAsync_Tests()
        {
            var chainBlockLinks = _chainBlockLinkService.GetCachedChainBlockLinks();
            Assert.Throws<InvalidOperationException>(() =>
            {
                AsyncHelper.RunSync(() => _chainBlockLinkService.SetChainBlockLinkExecutionStatusAsync(
                    chainBlockLinks[0].BlockHash,
                    ChainBlockLinkExecutionStatus.ExecutionNone));
            });

            var chainBlockLink1 =
                chainBlockLinks.First(cbl => cbl.ExecutionStatus != ChainBlockLinkExecutionStatus.ExecutionNone);
            Assert.Throws<InvalidOperationException>(() =>
            {
                AsyncHelper.RunSync(() => _chainBlockLinkService.SetChainBlockLinkExecutionStatusAsync(
                    chainBlockLink1.BlockHash,
                    ChainBlockLinkExecutionStatus.ExecutionNone));
            });
            var chainBlockLink2 =
                chainBlockLinks.First(cbl => cbl.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionNone);

            AsyncHelper.RunSync(() => _chainBlockLinkService.SetChainBlockLinkExecutionStatusAsync(
                chainBlockLink2.BlockHash,
                ChainBlockLinkExecutionStatus.ExecutionSuccess));
            chainBlockLink2.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionSuccess);
        }
    }
}