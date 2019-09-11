using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusServiceTests : ConsensusTestBase
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainService _blockchainService;

        public ConsensusServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _consensusService = GetRequiredService<IConsensusService>();
        }

        [Fact]
        public async Task ValidateConsensusBeforeExecutionAsync_Test()
        {
            var chainContext = await GetDefaultChainContext();
            
            var result = await _consensusService.ValidateConsensusBeforeExecutionAsync(chainContext, new byte[]{});
            result.ShouldBeFalse();
            
            var result1 = await _consensusService.ValidateConsensusBeforeExecutionAsync(chainContext, new byte[]{0, 1});
            result1.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateConsensusAfterExecutionAsync_Test()
        {
            var chainContext = await GetDefaultChainContext();
            
            var result = await _consensusService.ValidateConsensusAfterExecutionAsync(chainContext, new byte[]{});
            result.ShouldBeFalse();
            
            var result1 = await _consensusService.ValidateConsensusAfterExecutionAsync(chainContext, new byte[]{0, 1});
            result1.ShouldBeTrue();
        }

        [Fact]
        public async Task GetInformationToUpdateConsensusAsync_Test()
        {
            var chainContext = await GetDefaultChainContext();

            await TriggerConsensusAsync();
            var result = await _consensusService.GetConsensusExtraDataAsync(chainContext);
            result.ShouldNotBeNull();
        }

        [Fact]
        public async Task GenerateConsensusTransactionsAsync_Test()
        {
            var chainContext = await GetDefaultChainContext();

            await TriggerConsensusAsync();
            var result = await _consensusService.GenerateConsensusTransactionsAsync(chainContext);
            result.Count().ShouldBe(1);
        }

        private async Task TriggerConsensusAsync()
        {
            var chainContext = await GetDefaultChainContext();
            await _consensusService.TriggerConsensusAsync(chainContext);
        }
        
        private async Task<ChainContext> GetDefaultChainContext()
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