using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS4;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
        public Task GetTriggerInformationForBlockHeaderExtraData_ConsensusCommand_Test()
        {
            var result =
                _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(
                    consensusCommandBytes: new BytesValue());
            var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(data: result.Value);
            triggerInformation.Behaviour.ShouldBe(expected: AElfConsensusBehaviour.UpdateValue);

            result = _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(
                consensusCommandBytes: new ConsensusCommand
                        {Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.Nothing}.ToByteString()}
                    .ToBytesValue());
            triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(data: result.Value);
            triggerInformation.Behaviour.ShouldBe(expected: AElfConsensusBehaviour.Nothing);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetCurrentMinerList_Test()
        {
            var result = await _aedpoSInformationProvider.GetCurrentMinerList(new ChainContext());
            result.Count().ShouldBe(3);
        }
    }
}