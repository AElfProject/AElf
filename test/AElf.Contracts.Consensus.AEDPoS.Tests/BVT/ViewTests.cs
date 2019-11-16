using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        [Fact(Skip = "Need to be refactored via testkit aedpos extension")]
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

            var transactionResult = await AEDPoSContractStub.NextRound.SendAsync(nextTermInformation.Round);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            roundNumber = await AEDPoSContractStub.GetCurrentRoundNumber.CallAsync(new Empty());
            roundNumber.Value.ShouldBe(2);

            roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            roundInfo.RoundNumber.ShouldBe(2);

            var roundInformation = await AEDPoSContractStub.GetRoundInformation.CallAsync(new SInt64Value
            {
                Value = 2
            });
            roundInformation.RoundNumber.ShouldBe(2);
            roundInformation.TermNumber.ShouldBe(1);

            var currentTermRemainSeconds =
                (await AEDPoSContractStub.GetNextElectCountDown.CallAsync(new Empty())).Value;
            currentTermRemainSeconds.ShouldBeGreaterThan(0);
            currentTermRemainSeconds.ShouldBeLessThan(604800);

            //get term number
            var termNumber = await AEDPoSContractStub.GetCurrentTermNumber.CallAsync(new Empty());
            termNumber.Value.ShouldBe(1);
        }
    }
}