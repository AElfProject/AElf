using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using GetWelfareRewardAmountSampleInput = AElf.Contracts.Treasury.GetWelfareRewardAmountSampleInput;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task GetWelfareRewardAmountSample_Test()
        {
            await NextTerm(BootMinerKeyPair);
            await AttendElectionAndVotes();
            await ProduceBlocks(BootMinerKeyPair, 20);

            await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                Amount = 100_00000000,
                Period = 1,
                Symbol = EconomicContractsTestConstants.NativeTokenSymbol,
                SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare]
            });
            await NextTerm(BootMinerKeyPair);

            const int lockTime1 = 100 * 24 * 60 * 60;
            const int lockTime2 = 200 * 24 * 60 * 60;
            const int lockTime3 = 500 * 24 * 60 * 60;
            var rewardAmount = await TreasuryContractStub.GetWelfareRewardAmountSample.CallAsync(
                new GetWelfareRewardAmountSampleInput
                {
                    Value = {lockTime1, lockTime2, lockTime3}
                });
            rewardAmount.Value.Count.ShouldBe(3);
            
            var rewardMoney = rewardAmount.Value.ToArray();
            rewardMoney[0].ShouldBeGreaterThan(0);
            rewardMoney[1].ShouldBeGreaterThan(0);
            rewardMoney[2].ShouldBeGreaterThan(0);
            
        }

        private async Task AttendElectionAndVotes()
        {
            //announce election
            foreach (var user in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount))
            {
                var electionTester = GetElectionContractTester(user);
                var electionResult = await electionTester.AnnounceElection.SendAsync(new Empty());
                electionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var candidates = (await ElectionContractStub.GetCandidates.CallAsync(new Empty()))
                .Value.Select(o=>o.ToHex()).ToArray();
            
            //vote candidate
            for (var i = 0; i< EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
            {
                var electionTester = GetElectionContractTester(VoterKeyPairs[i]);
                var voteResult = await electionTester.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = candidates[i],
                    Amount = 10000,
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100 + 100 * i)
                });
                voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }
    }
}