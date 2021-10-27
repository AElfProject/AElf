using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests
    {
        [Fact]
        public async Task VoteContract_TakeSnapshot_WithoutPermission_Test()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 1);

            var otherUser = Accounts[2].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).TakeSnapshot.SendWithExceptionAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 1
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Only sponsor can take snapshot.");
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_WithoutVotingItem_Test()
        {
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendWithExceptionAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = HashHelper.ComputeFrom("hash"),
                    SnapshotNumber = 1
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting item not found").ShouldBeTrue();
        }
        
        [Fact]
        public async Task VoteContract_TakeSnapshot_Exceed_TotalSnapshotNumber_Test()
        {
            var totalSnapshotNumber = 1;
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, totalSnapshotNumber);
            await VoteContractStub.TakeSnapshot.SendAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 1
                });
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendWithExceptionAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 2
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Current voting item already ended.").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_WithWrongSnapshotNumber_Test()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 2);
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendWithExceptionAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 2
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Can only take snapshot of current snapshot number").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_Success_Test()
        {
            var registerItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 3);
            for (var i = 0; i < 3; i++)
            {
                var transactionResult = (await VoteContractStub.TakeSnapshot.SendAsync(
                    new TakeSnapshotInput
                    {
                        VotingItemId = registerItem.VotingItemId,
                        SnapshotNumber = i + 1
                    })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingItem = await GetVoteItem(registerItem.VotingItemId);
                votingItem.CurrentSnapshotNumber.ShouldBe(i + 2);
                var voteResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    SnapshotNumber = i + 2
                });
                voteResult.SnapshotNumber.ShouldBe(i + 2);
            }
        }
    }
}