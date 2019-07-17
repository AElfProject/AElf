using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task GetMinersCount()
        {
            await ElectionContract_AnnounceElection();

            var minersCount = await ElectionContractStub.GetMinersCount.CallAsync(new Empty());
            minersCount.Value.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
        }

        [Fact]
        public async Task GetElectionResult()
        {
            await ElectionContract_Vote();
            await NextTerm(InitialCoreDataCenterKeyPairs[0]);

            //verify term 1
            var electionResult = await ElectionContractStub.GetElectionResult.CallAsync(new GetElectionResultInput
            {
                TermNumber = 1
            });
            electionResult.IsActive.ShouldBe(false);
            electionResult.Results.Count.ShouldBe(19);
            electionResult.Results.Values.ShouldAllBe(o => o == 1000);
        }

        [Fact]
        public async Task GetElectorVoteWithRecords_NotExist()
        {
            await ElectionContract_Vote();

            var voteRecords = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringInput
            {
                Value = ValidationDataCenterKeyPairs.Last().PublicKey.ToHex()
            });

            voteRecords.ShouldBe(new ElectorVote
            {
                Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs.Last().PublicKey)
            });
        }

        [Fact]
        public async Task GetElectorVoteWithAllRecords()
        {
            var voters = await UserVotesCandidate(2, 500, 100);
            var voterKeyPair = voters[0];
            //without withdraw
            var allRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(new StringInput
            {
                Value = voterKeyPair.PublicKey.ToHex()
            });
            allRecords.ActiveVotingRecords.Count.ShouldBeGreaterThanOrEqualTo(1);
            allRecords.WithdrawnVotingRecordIds.Count.ShouldBe(0);

            //withdraw
            await NextTerm(InitialCoreDataCenterKeyPairs[0]);
            BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(100 * 60 * 60 * 24 + 1));
            var voteId =
                (await ElectionContractStub.GetElectorVote.CallAsync(new StringInput
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotingRecordIds.First();
            var executionResult = await WithdrawVotes(voterKeyPair, voteId);
            executionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            allRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(new StringInput
            {
                Value = voterKeyPair.PublicKey.ToHex()
            });
            allRecords.WithdrawnVotingRecordIds.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetVotersCount()
        {
            await UserVotesCandidate(5, 1000, 120);

            var votersCount = await ElectionContractStub.GetVotersCount.CallAsync(new Empty());
            votersCount.Value.ShouldBe(5 * CandidatesCount);
        }

        [Fact]
        public async Task GetVotesAmount()
        {
            await UserVotesCandidate(2, 200, 120);

            var votesAmount = await ElectionContractStub.GetVotesAmount.CallAsync(new Empty());
            votesAmount.Value.ShouldBe(2 * CandidatesCount * 200);
        }

        [Fact]
        public async Task GetTermSnapshot_Test()
        {
            //first round
            {
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[0], 5);
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[2], 15);
                await NextTerm(BootMinerKeyPair);

                var snapshot = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
                {
                    TermNumber = 1
                });
                snapshot.MinedBlocks.ShouldBeGreaterThanOrEqualTo(30);
                snapshot.ElectionResult.Count.ShouldBe(0);
            }

            //second round
            {
                ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

                var moreVotesCandidates = ValidationDataCenterKeyPairs
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                moreVotesCandidates.ForEach(async kp =>
                    await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

                await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
                await NextTerm(BootMinerKeyPair);

                var snapshot = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
                {
                    TermNumber = 2
                });
                snapshot.MinedBlocks.ShouldBeGreaterThanOrEqualTo(10);
                snapshot.ElectionResult.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);
                snapshot.ElectionResult.Values
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToArray()
                    .ShouldAllBe(item => item == 2);
            }
        }

        [Fact]
        public async Task GetPageableCandidateInformation()
        {
            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);
            var moreVotesCandidates = ValidationDataCenterKeyPairs
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));
            var fewVotesCandidates = ValidationDataCenterKeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount).Take(10).ToList();
            fewVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            var candidateInformation =
                await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
                {
                    Start = 0,
                    Length = 5
                });
            candidateInformation.Value.Count.ShouldBe(5);
            candidateInformation.Value.ToList().Select(o => o.ObtainedVotesAmount).ShouldAllBe(o => o == 2);

            var candidateInformation1 =
                await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
                {
                    Start = 5,
                    Length = 10
                });
            candidateInformation1.Value.Count.ShouldBe(10);
            candidateInformation1.Value.ToList().Select(o => o.ObtainedVotesAmount).ShouldAllBe(o => o == 1);
        }

        [Fact]
        public async Task GetCurrentMiningReward()
        {
            await NextTerm(BootMinerKeyPair);

            //basic value
            {
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
                var miningReward = await ElectionContractStub.GetCurrentMiningReward.CallAsync(new Empty());
                miningReward.Value.ShouldBeGreaterThanOrEqualTo(ElectionContractConstants.ElfTokenPerBlock * 10);
            }

            //compare with different term
            {
                await NextTerm(BootMinerKeyPair);
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
                var miningReward1 = await ElectionContractStub.GetCurrentMiningReward.CallAsync(new Empty());

                await NextTerm(BootMinerKeyPair);
                await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
                var miningReward2 = await ElectionContractStub.GetCurrentMiningReward.CallAsync(new Empty());

                miningReward1.ShouldBe(miningReward2);
            }
        }

        private async Task<List<ECKeyPair>> UserVotesCandidate(int voterCount, long voteAmount, int lockDays)
        {
            var lockTime = lockDays * 60 * 60 * 24;

            var candidatesKeyPairs = await ElectionContract_AnnounceElection();

            var votersKeyPairs = VoterKeyPairs.Take(voterCount).ToList();
            var voterKeyPair = votersKeyPairs[0];
            var balanceBeforeVoting = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            balanceBeforeVoting.ShouldBeGreaterThan(0);

            await VoteToCandidates(votersKeyPairs,
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, voteAmount);

            return votersKeyPairs;
        }
    }
}