using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class AEDPoSTest
{
    [Fact]
    public async Task Query_RoundInformation_Test()
    {
        //first round
        var roundNumber = await AEDPoSContractStub.GetCurrentRoundNumber.CallAsync(new Empty());

        var roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
        roundInfo.TermNumber.ShouldBe(roundNumber.Value);

        //second round            
        var nextTermInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
            new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.NextRound,
                Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
            }.ToBytesValue())).ToConsensusHeaderInformation();

        var nextRoundInput = NextRoundInput.Parser.ParseFrom(nextTermInformation.Round.ToByteArray());
        var randomNumber = await GenerateRandomProofAsync(BootMinerKeyPair);
        nextRoundInput.RandomNumber = ByteString.CopyFrom(randomNumber);
        var transactionResult = await AEDPoSContractStub.NextRound.SendAsync(nextRoundInput);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        roundNumber = await AEDPoSContractStub.GetCurrentRoundNumber.CallAsync(new Empty());
        roundNumber.Value.ShouldBe(2);

        roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
        roundInfo.RoundNumber.ShouldBe(2);

        var previousRoundInfo = await AEDPoSContractStub.GetPreviousRoundInformation.CallAsync(new Empty());
        previousRoundInfo.RoundNumber.ShouldBe(1);

        var roundInformation = await AEDPoSContractStub.GetRoundInformation.CallAsync(new Int64Value
        {
            Value = 2
        });
        roundInformation.RoundNumber.ShouldBe(2);
        roundInformation.TermNumber.ShouldBe(1);

        var currentTermRemainSeconds =
            (await AEDPoSContractStub.GetNextElectCountDown.CallAsync(new Empty())).Value;
        currentTermRemainSeconds.ShouldBeGreaterThan(0);
        currentTermRemainSeconds.ShouldBe(604800);

        //get term number
        var termNumber = await AEDPoSContractStub.GetCurrentTermNumber.CallAsync(new Empty());
        termNumber.Value.ShouldBe(1);
    }

    [Fact]
    public async Task GetCurrentMinerPubkeyList_Test()
    {
        var pubkeyList = await AEDPoSContractStub.GetCurrentMinerPubkeyList.CallAsync(new Empty());
        pubkeyList.Pubkeys.Count.ShouldBe(5);

        var miners = await AEDPoSContractStub.GetMinerList.CallAsync(new GetMinerListInput
        {
            TermNumber = 1
        });
        var pubKeys = miners.Pubkeys.Select(o => o.ToHex()).ToList();
        pubKeys.Count.ShouldBe(5);
        pubKeys.ShouldBe(pubkeyList.Pubkeys);
    }

    [Fact]
    public async Task GetNextMinerPubkey_Test()
    {
        var pubkeyList = await AEDPoSContractStub.GetCurrentMinerPubkeyList.CallAsync(new Empty());
        var nextMiner = await AEDPoSContractStub.GetNextMinerPubkey.CallAsync(new Empty());
        pubkeyList.Pubkeys.ShouldContain(nextMiner.Value);
    }

    [Fact]
    public async Task GetCurrentMinerList_Test()
    {
        var minerList = await AEDPoSContractStub.GetMinerList.CallAsync(new GetMinerListInput { TermNumber = 1 });
        minerList.Pubkeys.Count.ShouldBe(5);

        var currentMinerListWithRoundNumber =
            await AEDPoSContractStub.GetCurrentMinerListWithRoundNumber.CallAsync(new Empty());
        currentMinerListWithRoundNumber.MinerList.Pubkeys.Count.ShouldBe(5);
        currentMinerListWithRoundNumber.RoundNumber.ShouldBe(1);

        var nextMiner = await AEDPoSContractStub.GetNextMinerPubkey.CallAsync(new Empty());
        minerList.Pubkeys.Select(k => k.ToHex()).ShouldContain(nextMiner.Value);
    }
}