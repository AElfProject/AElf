using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        [Fact]
        public async Task VoteContract_TakeSnapshot_WithoutPermission()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 1);

            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var transactionResult = (await GetVoteContractTester(otherUser).TakeSnapshot.SendAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 1
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_WithoutVotingItem()
        {
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = Hash.FromString("hash"),
                    SnapshotNumber = 1
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting item not found").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_WithWrongSnapshotNumber()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 2);
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendAsync(
                new TakeSnapshotInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    SnapshotNumber = 2
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Can only take snapshot of current snapshot number").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_TakeSnapshot_Success()
        {
            var registerItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 3);
            for (int i = 0; i < 3; i++)
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
            }
        }
    }
}