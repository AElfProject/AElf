using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner.Application;
using Xunit;

namespace AElf.Kernel
{
    public class BlockMiningEventHandlerTests : KernelWithChainTestBase
    {
        private IBlockchainService _chainService;
        private ConsensusRequestMiningEventHandler _miningEventHandler;

        public BlockMiningEventHandlerTests()
        {
            _chainService = GetRequiredService<IBlockchainService>();
            _miningEventHandler = GetRequiredService<ConsensusRequestMiningEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsyncTest()
        {
            var chain = await _chainService.GetChainAsync();
            var hash = chain.BestChainHash;
            var height = chain.BestChainHeight;
            var eventData =
                new ConsensusRequestMiningEventData(hash, height, DateTime.UtcNow,
                    TimeSpan.FromMilliseconds(60 * 1000));

            await _miningEventHandler.HandleEventAsync(eventData);
        }
    }
}