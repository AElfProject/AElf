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
            const int lastDays = 10;

            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = 1,
                StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                EndTimestamp = DateTime.UtcNow.AddDays(lastDays).ToTimestamp(),
                Options = { GenerateOptions(3) },
                AcceptedCurrency = TestTokenSymbol
            };

            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //register again
            transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Voting item already exists");
        }
        
        [Fact]
        public async Task VoteContract_GetVotingResult()
        {
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            var votingItem = await RegisterVotingItem(10, 3, true, DefaultSender, 2);
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