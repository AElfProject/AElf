using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class IrreversibleBlockDiscoveryServiceTests : AEDPoSTestBase
    {
        private IIrreversibleBlockDiscoveryService _irreversibleBlockDiscoveryService;
        private IBlockchainService _chainService;

        public IrreversibleBlockDiscoveryServiceTests()
        {
            _irreversibleBlockDiscoveryService = GetRequiredService<IIrreversibleBlockDiscoveryService>();
            _chainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task DiscoverAndSetIrreversibleAsync_Test()
        {
            var chain = await _chainService.GetChainAsync();
            var blockId = chain.LastIrreversibleBlockHash;
            var iblockIndex =
                await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, new[] {blockId});
            iblockIndex.Hash.ShouldBe(new Hash());
            iblockIndex.Height.ShouldBe(15);
        }
    }
}