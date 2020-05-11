using System.Linq;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class ChainBlockLinkCacheProviderTests : AElfKernelWithChainTestBase
    {
        private IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;
        private readonly KernelTestHelper _kernelTestHelper;

        public ChainBlockLinkCacheProviderTests()
        {
            _chainBlockLinkCacheProvider = GetRequiredService<IChainBlockLinkCacheProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public void Get_Set_ChainBlockLink_Test()
        {
            var hash = HashHelper.ComputeFrom("MyBlock");
            var chainBlockLink1 = new ChainBlockLink()
            {
                BlockHash = hash,
                Height = 20L,
                PreviousBlockHash = Hash.Empty,
                IsLinked = false,
                IsIrreversibleBlock = false
            };
            _chainBlockLinkCacheProvider.SetChainBlockLink(chainBlockLink1);
            var chainBlockLink2 = _chainBlockLinkCacheProvider.GetChainBlockLink(hash);
            chainBlockLink2.ShouldBe(chainBlockLink1);
        }

        [Fact]
        public void GetChainBlockLinks_Test()
        {
            var bestCount = _kernelTestHelper.BestBranchBlockList.Count;
            var longestCount = _kernelTestHelper.LongestBranchBlockList.Count;
            var forkCount = _kernelTestHelper.ForkBranchBlockList.Count;
            var unlinkedCount = _kernelTestHelper.UnlinkedBranchBlockList.Count;
            var chainBlockLinks = _chainBlockLinkCacheProvider.GetChainBlockLinks();
            chainBlockLinks.Count.ShouldBe(bestCount + longestCount + forkCount + unlinkedCount);
        }

        [Fact]
        public void RemoveChainBlockLink()
        {
            var chainBlockLinks = _chainBlockLinkCacheProvider.GetChainBlockLinks();
            if (!chainBlockLinks.Any())
                return;
            var hash = chainBlockLinks[^1].BlockHash;
            _chainBlockLinkCacheProvider.RemoveChainBlockLink(hash);
            _chainBlockLinkCacheProvider.GetChainBlockLink(hash).ShouldBeNull();
        }
    }
}