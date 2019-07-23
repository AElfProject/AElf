using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class BestChainFoundEventHandlerTests : AEDPoSTestBase
    {
        private readonly BestChainFoundEventHandler _foundEventHandler;

        public BestChainFoundEventHandlerTests()
        {
            _foundEventHandler = GetRequiredService<BestChainFoundEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync_Test()
        {
            var eventData = new BestChainFoundEventData
            {
                BlockHash = Hash.FromString("BlockHash"),
                BlockHeight = 11L,
                ExecutedBlocks = new List<Hash>
                {
                    Hash.FromString("element1"),
                    Hash.FromString("element2"),
                }
            };
            
            await _foundEventHandler.HandleEventAsync(eventData);
        }
    }
}