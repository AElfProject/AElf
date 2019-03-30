using System.Threading.Tasks;
using AElf.Consensus.DPoS;
using AElf.Kernel.Consensus.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus
{
    public class ConsensusServiceTests : ConsensusTestBase
    {
        private IConsensusService _consensusService;
        public ConsensusServiceTests()
        {
            _consensusService = GetRequiredService<IConsensusService>();
        }

        [Fact]
        public async Task GetNewConsensusInformationAsync()
        {
            var bytes = await _consensusService.GetNewConsensusInformationAsync();
            var dposTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(bytes);
            dposTriggerInformation.IsBootMiner.ShouldBeFalse();
        }
        
    }
}