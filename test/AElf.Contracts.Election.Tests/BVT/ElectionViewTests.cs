using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
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
            minersCount.Value.ShouldBe(InitialMinersCount);
        }

        [Fact]
        public async Task GetElectionResult()
        {
            await ElectionContract_Vote();
            await NextTerm(InitialMinersKeyPairs[0]);

            //verify term 1
            var electionResult = await ElectionContractStub.GetElectionResult.CallAsync(new GetElectionResultInput
            {
                TermNumber = 1
            });
            electionResult.IsActive.ShouldBe(false);
            electionResult.Results.Count.ShouldBe(19);
            electionResult.Results.Values.ShouldAllBe(o => o==1000);
        }

        [Fact]
        public async Task GetElectorVoteWithRecords_NotExist()
        {
            await ElectionContract_Vote();

            var voteRecords = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringInput
            {
                Value = FullNodesKeyPairs.Last().PublicKey.ToHex()
            });
            
            voteRecords.ShouldBe(new ElectorVote
            {
                PublicKey = ByteString.CopyFrom(FullNodesKeyPairs.Last().PublicKey)
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
            await NextTerm(InitialMinersKeyPairs[0]);
            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime().AddSeconds(100*60*60*24 + 1));
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
            votersCount.Value.ShouldBe(5*CandidatesCount);
        }

        [Fact]
        public async Task GetVotesAmount()
        {
            await UserVotesCandidate(2, 200, 120);
            
            var votesAmount = await ElectionContractStub.GetVotesAmount.CallAsync(new Empty());
            votesAmount.Value.ShouldBe(2*CandidatesCount*200);
        }

        private async Task<List<ECKeyPair>> UserVotesCandidate(int voterCount, long voteAmount, int lockDays)
        {
            var lockTime = lockDays * 60 * 60 * 24;

            var candidatesKeyPairs = await ElectionContract_AnnounceElection();

            var votersKeyPairs = VotersKeyPairs.Take(voterCount).ToList();
            var voterKeyPair = votersKeyPairs[0];
            var balanceBeforeVoting = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            balanceBeforeVoting.ShouldBeGreaterThan(0);

            await VoteToCandidates(votersKeyPairs,
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, voteAmount);
           
            return votersKeyPairs;
        }
    }
}