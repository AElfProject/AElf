using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class AEDPoSTriggerInformationProviderTests : AEDPoSTestBase
    {
        private ITriggerInformationProvider _triggerInformationProvider;
        public AEDPoSTriggerInformationProviderTests()
        {
            _triggerInformationProvider = GetRequiredService<ITriggerInformationProvider>();
        }

        [Fact]
        public async Task GetTriggerInformationForBlockHeaderExtraDataAsync_NullConsensusCommand()
        {
            var result = await _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraDataAsync(null);
            var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
            triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
        }

        [Fact]
        public async Task GetTriggerInformationForConsensusTransactionsAsync_NullConsensusCommand()
        {
            var result = await _triggerInformationProvider.GetTriggerInformationForConsensusTransactionsAsync(null);
            var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
            triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
        }
    }
}