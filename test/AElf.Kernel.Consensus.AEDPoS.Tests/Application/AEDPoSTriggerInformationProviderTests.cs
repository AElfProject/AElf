using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application;

public class AEDPoSTriggerInformationProviderTests : AEDPoSTestBase
{
    private readonly IAEDPoSInformationProvider _aedpoSInformationProvider;
    private readonly ITriggerInformationProvider _triggerInformationProvider;

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
                new BytesValue());
        var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
        triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);

        result = _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(
            new ConsensusCommand
                    { Hint = new AElfConsensusHint { Behaviour = AElfConsensusBehaviour.Nothing }.ToByteString() }
                .ToBytesValue());
        triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
        triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.Nothing);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCurrentMinerList_Test()
    {
        var result = await _aedpoSInformationProvider.GetCurrentMinerListAsync(new ChainContext());
        result.Count().ShouldBe(3);
    }

    [Fact]
    public void GetTriggerInformationForBlockHeaderExtraData_CommandIsNull_Test()
    {
        var result =
            _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(null);
        var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
        triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
    }

    [Fact]
    public void GetTriggerInformationForConsensusTransactions_CommandIsNull_Test()
    {
        var result =
            _triggerInformationProvider.GetTriggerInformationForConsensusTransactions(new ChainContext(), null);
        var triggerInformation = AElfConsensusTriggerInformation.Parser.ParseFrom(result.Value);
        triggerInformation.Behaviour.ShouldBe(AElfConsensusBehaviour.UpdateValue);
    }
}