using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner.Application;
using Google.Protobuf.WellKnownTypes;
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
                new ConsensusRequestMiningEventData(hash, height, TimestampHelper.GetUtcNow(),
                    TimestampHelper.DurationFromSeconds(60));

            await _miningEventHandler.HandleEventAsync(eventData);
        }
    }
}