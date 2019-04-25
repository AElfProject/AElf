using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public class ConsensusCommonMethodsTests : AElfConsensusContractTestBase
    {
        public ConsensusCommonMethodsTests()
        {
            InitializeContracts();
        }
        
        /// <summary>
        /// For now the information of first round will be filled in first block,
        /// which means this information should exist before mining process starting.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CheckFirstRound()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            firstRound.RoundNumber.ShouldBe(1);
            firstRound.RealTimeMinersInformation.Count.ShouldBe(InitialMinersCount);
            firstRound.GetMiningInterval().ShouldBe(MiningInterval);
        }
    }
}