using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner.Application
{
    public class MiningRequestServiceTests: KernelMiningTestBase
    {
        private readonly IMiningRequestService _miningRequestService;
        private readonly IBlockchainService _blockchainService;

        public MiningRequestServiceTests()
        {
            _miningRequestService = GetRequiredService<IMiningRequestService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task RequestMining_Test()
        {
            var chain = await _blockchainService.GetChainAsync();

            var request = new ConsensusRequestMiningDto
            {
                BlockTime = TimestampHelper.GetUtcNow(),
                BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(500),
                MiningDueTime = TimestampHelper.GetUtcNow().AddMilliseconds(499),
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight
            };

            var block = await _miningRequestService.RequestMiningAsync(request);
            block.ShouldBeNull();

            request = new ConsensusRequestMiningDto
            {
                BlockTime = TimestampHelper.GetUtcNow().AddMilliseconds(-501),
                BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(500),
                MiningDueTime = TimestampHelper.GetUtcNow().AddMilliseconds(350),
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight
            };
            block = await _miningRequestService.RequestMiningAsync(request);
            block.ShouldBeNull();
            
            request = new ConsensusRequestMiningDto
            {
                BlockTime = TimestampHelper.GetUtcNow().AddMilliseconds(-400),
                BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(500),
                MiningDueTime = TimestampHelper.GetUtcNow().AddMilliseconds(350),
                PreviousBlockHash = chain.BestChainHash,
                PreviousBlockHeight = chain.BestChainHeight
            };
            block = await _miningRequestService.RequestMiningAsync(request);
            block.ShouldNotBeNull();
            block.Header.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            block.Header.Height.ShouldBe(chain.BestChainHeight + 1);
            block.Header.Time.ShouldBe(request.BlockTime);
        }
    }
}