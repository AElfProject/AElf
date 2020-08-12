using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using Autofac.Core;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel
{
    public class ConsensusRequestMiningEventHandlerTests : KernelConsensusRequestMiningTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ConsensusRequestMiningEventHandler _consensusRequestMiningEventHandler;
        private readonly ILocalEventBus _localEventBus;
        private readonly KernelConsensusRequestMiningTestContext _testContext;

        public ConsensusRequestMiningEventHandlerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _consensusRequestMiningEventHandler = GetRequiredService<ConsensusRequestMiningEventHandler>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
            _testContext = GetRequiredService<KernelConsensusRequestMiningTestContext>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            BlockMinedEventData blockMinedEventData = null;
            _localEventBus.Subscribe<BlockMinedEventData>(d =>
            {
                blockMinedEventData = d;
                return Task.CompletedTask;
            });
            
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHash = chain.BestChainHash;
            var bestChainHeight = chain.BestChainHeight;
            
            _testContext.MockConsensusService.Verify(
                s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Exactly(10));

            {
                var eventData = new ConsensusRequestMiningEventData(HashHelper.ComputeFrom("NotBestChain"),
                    bestChainHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(500),
                    TimestampHelper.GetUtcNow().AddMilliseconds(499));

                await HandleConsensusRequestMiningEventAsync(eventData);
                blockMinedEventData.ShouldBeNull();
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(bestChainHeight);
                chain.BestChainHash.ShouldBe(bestChainHash);

                _testContext.MockConsensusService.Verify(
                    s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Exactly(10));
            }

            {
                var eventData = new ConsensusRequestMiningEventData(bestChainHash, bestChainHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(500),
                    TimestampHelper.GetUtcNow().AddMilliseconds(499));

                await HandleConsensusRequestMiningEventAsync(eventData);
                blockMinedEventData.ShouldBeNull();
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(bestChainHeight);
                chain.BestChainHash.ShouldBe(bestChainHash);
                
                _testContext.MockConsensusService.Verify(
                    s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Exactly(11));
            }
            
            {
                var eventData = new ConsensusRequestMiningEventData(bestChainHash, bestChainHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(500),
                    TimestampHelper.GetUtcNow().AddSeconds(30));

                await HandleConsensusRequestMiningEventAsync(eventData);
                blockMinedEventData.ShouldNotBeNull();
                blockMinedEventData.BlockHeader.Height.ShouldBe(bestChainHeight +1);
                blockMinedEventData.BlockHeader.PreviousBlockHash.ShouldBe(bestChainHash);
                
                chain = await _blockchainService.GetChainAsync();
                chain.Branches.ShouldContainKey(blockMinedEventData.BlockHeader.GetHash().ToStorageKey());
                
                (await _blockchainService.HasBlockAsync(blockMinedEventData.BlockHeader.GetHash())).ShouldBeTrue();
                
                _testContext.MockConsensusService.Verify(
                    s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Exactly(11));
            }
        }

        private async Task HandleConsensusRequestMiningEventAsync(
            ConsensusRequestMiningEventData consensusRequestMiningEventData)
        {
            await _consensusRequestMiningEventHandler.HandleEventAsync(consensusRequestMiningEventData);
            await Task.Delay(500);
        }
    }
}