using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using Xunit;

namespace AElf.Kernel
{
    public class BlockMiningEventHandlerTests : KernelWithoutConsensusStaffTestBase
    {
        private IBlockchainService _chainService;
        private ConsensusRequestMiningEventHandler _miningEventHandler;

        public BlockMiningEventHandlerTests()
        {
            _chainService = GetRequiredService<IBlockchainService>();
            _miningEventHandler = GetRequiredService<ConsensusRequestMiningEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync_Test()
        {
            var chain = await _chainService.GetChainAsync();
            var hash = chain.BestChainHash;
            var height = chain.BestChainHeight;
            var eventData =
                new ConsensusRequestMiningEventData(hash, height, TimestampHelper.GetUtcNow(),
                    TimestampHelper.DurationFromSeconds(60), TimestampHelper.GetUtcNow().AddDays(1));

            await _miningEventHandler.HandleEventAsync(eventData);
        }
    }
}