using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.Consensus.Application
{
    public sealed class ConsensusServiceTests : ConsensusTestBase
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockTimeProvider _blockTimeProvider;

        private ChainContext ChainContext => AsyncHelper.RunSync(GetDefaultChainContextAsync);

        public ConsensusServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _consensusService = GetRequiredService<IConsensusService>();
            _blockTimeProvider = GetRequiredService<IBlockTimeProvider>();
        }

        [Fact]
        public async Task TriggerConsensusAsync_Test()
        {
            await _consensusService.TriggerConsensusAsync(ChainContext);

            // Check BlockTimeProvider.
            var blockTime = _blockTimeProvider.GetBlockTime(Hash.Empty);
            blockTime.ShouldNotBeNull();

            // Check whether consensus scheduler is filled.
            var consensusTestHelper = GetRequiredService<IConsensusTestHelper>();
            consensusTestHelper.IsConsensusSchedulerFilled.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateConsensusBeforeExecutionAsync_Test()
        {
            var chainContext = ChainContext;

            (await _consensusService.ValidateConsensusBeforeExecutionAsync(chainContext, new byte[] { }))
                .ShouldBeFalse();
            (await _consensusService.ValidateConsensusBeforeExecutionAsync(chainContext, new byte[] {0, 1}))
                .ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateConsensusAfterExecutionAsync_Test()
        {
            var chainContext = ChainContext;

            (await _consensusService.ValidateConsensusAfterExecutionAsync(chainContext, new byte[] { }))
                .ShouldBeFalse();
            (await _consensusService.ValidateConsensusAfterExecutionAsync(chainContext, new byte[] {0, 1}))
                .ShouldBeTrue();
        }

        [Fact]
        public async Task GetConsensusExtraDataAsyncAsync_Test()
        {
            await TriggerConsensusAsync();
            var result = await _consensusService.GetConsensusExtraDataAsync(ChainContext);
            result.ShouldNotBeNull();
        }

        [Fact]
        public async Task GenerateConsensusTransactionsAsync_Test()
        {
            await TriggerConsensusAsync();
            var result = await _consensusService.GenerateConsensusTransactionsAsync(ChainContext);
            result.Count.ShouldBe(1);
        }

        private async Task TriggerConsensusAsync()
        {
            var chainContext = await GetDefaultChainContextAsync();
            await _consensusService.TriggerConsensusAsync(chainContext);
        }

        private async Task<ChainContext> GetDefaultChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return chainContext;
        }
    }
}