using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class AEDPoSTriggerInformationProviderTests : AEDPoSTestBase
    {
        private ITriggerInformationProvider _triggerInformationProvider;
        private IAEDPoSInformationProvider _aedpoSInformationProvider;
        public AEDPoSTriggerInformationProviderTests()
        {
            _triggerInformationProvider = GetRequiredService<ITriggerInformationProvider>();
            _aedpoSInformationProvider = GetRequiredService<IAEDPoSInformationProvider>();
        }

        [Fact]
        public async Task GetTriggerInformationForBlockHeaderExtraData_NullConsensusCommand_Test()
        {
            var result = _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(null);
            var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
            triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
        }

        [Fact]
        public async Task GetTriggerInformationForConsensusTransactions_NullConsensusCommand_Test()
        {
            var result = _triggerInformationProvider.GetTriggerInformationForConsensusTransactions(null);
            var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
            triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
        }

        [Fact]
        public async Task GetCurrentMinerList_Test()
        {
            var result = await _aedpoSInformationProvider.GetCurrentMinerList(new ChainContext());
            result.Count().ShouldBe(3);
        }
    }
}