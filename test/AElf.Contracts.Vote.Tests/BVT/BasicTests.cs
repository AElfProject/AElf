using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Vote;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        [Fact]
        public async Task VoteContract_Register()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 10);

            // Check voting item according to the input.
            (votingItem.EndTimestamp.ToDateTime() - votingItem.StartTimestamp.ToDateTime()).TotalDays.ShouldBe(10);
            votingItem.Options.Count.ShouldBe(4);
            votingItem.Sponsor.ShouldBe(DefaultSender);
            votingItem.TotalSnapshotNumber.ShouldBe(10);
            
            // Check more about voting item.
            votingItem.CurrentSnapshotNumber.ShouldBe(1);
            votingItem.CurrentSnapshotStartTimestamp.ShouldBe(votingItem.StartTimestamp);
            votingItem.RegisterTimestamp.ShouldBeGreaterThan(votingItem.StartTimestamp);// RegisterTimestamp should be a bit later.
            
            // Check voting result of first period initialized.
            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                VotingItemId = votingItem.VotingItemId,
                SnapshotNumber = 1
            });
            votingResult.VotingItemId.ShouldBe(votingItem.VotingItemId);
            votingResult.SnapshotNumber.ShouldBe(1);
            votingResult.SnapshotStartTimestamp.ShouldBe(votingItem.StartTimestamp);
            votingResult.SnapshotEndTimestamp.ShouldBe(null);
            votingResult.Results.Count.ShouldBe(0);
            votingResult.VotersCount.ShouldBe(0);
        }

        [Fact]
        public async Task VoteContract_Vote()
        {
            
        }
        
        [Fact]
        public async Task VoteContract_Withdraw()
        {
            
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot()
        {
            
        }
        
        [Fact]
        public async Task VoteContract_AddOption()
        {
            
        }
        
        [Fact]
        public async Task VoteContract_RemoveOption()
        {
            
        }

        [Fact]
        public async Task VoteContract_GetVotingResult()
        {
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            var votingItem = await RegisterVotingItemAsync(10, 3, true, DefaultSender, 2);
            await Vote(voteUser, votingItem.VotingItemId, votingItem.Options.First(), 1000L);

            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                VotingItemId = votingItem.VotingItemId,
                SnapshotNumber = 1
            });
            
            votingResult.VotingItemId.ShouldBe(votingItem.VotingItemId);
            votingResult.VotersCount.ShouldBe(1);
            votingResult.Results.Values.First().ShouldBe(1000L);
        }
    }
}