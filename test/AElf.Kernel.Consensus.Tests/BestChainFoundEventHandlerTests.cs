using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus.Application;
using Xunit;

namespace AElf.Kernel.Consensus
{
    public class BestChainFoundEventHandlerTests : ConsensusTestBase
    {
        private BestChainFoundEventHandler _bestChainFoundEventHandler;
        private IConsensusService _consensusService;
        
        public BestChainFoundEventHandlerTests()
        {
            _consensusService = GetRequiredService<IConsensusService>();
            _bestChainFoundEventHandler = new BestChainFoundEventHandler(_consensusService);
        }

        [Fact]
        public async Task HandleEventAsyncTest()
        {
            var eventData = new BestChainFoundEventData
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 100
            };

            _bestChainFoundEventHandler.HandleEventAsync(eventData);
        }
    }
}