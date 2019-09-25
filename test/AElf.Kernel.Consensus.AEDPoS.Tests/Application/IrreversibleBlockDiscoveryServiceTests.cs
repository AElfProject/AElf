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
        // TODO:
/*        private IIrreversibleBlockRelatedEventsDiscoveryService _irreversibleBlockRelatedEventsDiscoveryService;
        private IBlockchainService _chainService;

        public IrreversibleBlockDiscoveryServiceTests()
        {
            _irreversibleBlockRelatedEventsDiscoveryService = GetRequiredService<IIrreversibleBlockRelatedEventsDiscoveryService>();
            _chainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task DiscoverAndSetIrreversibleAsync_Test()
        {
            var chain = await _chainService.GetChainAsync();
            var blockId = chain.LastIrreversibleBlockHash;
            var blockIndex =
                await _irreversibleBlockRelatedEventsDiscoveryService.GetLastIrreversibleBlockIndexAsync(chain, new[] {blockId});
            blockIndex.ShouldBeNull();
        }*/
    }
}