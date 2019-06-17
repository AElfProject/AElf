using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Xunit;

namespace AElf.Kernel.Consensus
{
    public class ConsensusServiceTests : AElfKernelConsensusTestBase
    {
        private IBlockchainService _chainService;
        private IConsensusService _consensusService;
        
        public ConsensusServiceTests()
        {
            _chainService = GetRequiredService<IBlockchainService>();
            _consensusService = GetRequiredService<IConsensusService>();
        }

        [Fact]
        public async Task TriggerConsensus_Test()
        {
            var chain = await _chainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            await _consensusService.TriggerConsensusAsync(chainContext);
        }
    }
}