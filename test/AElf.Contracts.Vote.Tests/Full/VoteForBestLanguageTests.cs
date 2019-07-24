using System.Linq;
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
        public async Task MultipleUsers_Vote_Scenario()
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 3);

            var user1 = SampleECKeyPairs.KeyPairs[1];
            var user2 = SampleECKeyPairs.KeyPairs[2];
            var user3 = SampleECKeyPairs.KeyPairs[3];

            //phase 1
            {
                //user1 vote 100
                var transactionResult1 = await Vote(user1, registerItem.VotingItemId, registerItem.Options[0], 100);
                transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);

                //user2 vote 150
                var transactionResult2 = await Vote(user2, registerItem.VotingItemId, registerItem.Options[0], 150);
                transactionResult2.Status.ShouldBe(TransactionResultStatus.Mined);

                //user3 vote 200
                var transactionResult3 = await Vote(user3, registerItem.VotingItemId, registerItem.Options[1], 200);
                transactionResult3.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingResult = await GetVotingResult(registerItem.VotingItemId, 1);
                votingResult.VotersCount.ShouldBe(3);
                votingResult.Results.Count.ShouldBe(2);
                votingResult.Results[registerItem.Options[0]].ShouldBe(250);
                votingResult.Results[registerItem.Options[1]].ShouldBe(200);

                //take snapshot
                var snapshotResult = await TakeSnapshot(registerItem.VotingItemId, 1);
                snapshotResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //query vote ids
                var voteIds = await GetVoteIds(user1, registerItem.VotingItemId);
                //query result
                var voteRecord = await GetVotingRecord(voteIds.ActiveVotes.First());
                voteRecord.Option.ShouldBe(registerItem.Options[0]);
                voteRecord.Amount.ShouldBe(100);

                //withdraw
                var beforeBalance = GetUserBalance(Address.FromPublicKey(user1.PublicKey));
                await Withdraw(user1, voteIds.ActiveVotes.First());
                var afterBalance = GetUserBalance(Address.FromPublicKey(user1.PublicKey));

                beforeBalance.ShouldBe(afterBalance - 100);

                voteIds = await GetVoteIds(user1, registerItem.VotingItemId);
                voteIds.ActiveVotes.Count.ShouldBe(0);
                voteIds.WithdrawnVotes.Count.ShouldBe(1);
            }

            //phase 2
            {
                //add some more option
                var options = new[]
                {
                    SampleAddress.AddressList[3].GetFormatted(),
                    SampleAddress.AddressList[4].GetFormatted(),
                    SampleAddress.AddressList[5].GetFormatted()
                };
                var optionResult = (await VoteContractStub.AddOptions.SendAsync(new AddOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options = {options}
                })).TransactionResult;
                optionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //user1 vote new option 1
                var transactionResult1 = await Vote(user1, registerItem.VotingItemId, options[0], 100);
                transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);

                //user2 vote new option 2
                var transactionResult2 = await Vote(user2, registerItem.VotingItemId, options[1], 100);
                transactionResult2.Status.ShouldBe(TransactionResultStatus.Mined);

                //user3 vote new option 3 twice
                var transactionResult3 = await Vote(user3, registerItem.VotingItemId, options[2], 100);
                transactionResult3.Status.ShouldBe(TransactionResultStatus.Mined);
                transactionResult3 = await Vote(user3, registerItem.VotingItemId, options[2], 100);
                transactionResult3.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingResult = await GetVotingResult(registerItem.VotingItemId, 2);
                votingResult.VotersCount.ShouldBe(7);
                votingResult.Results.Count.ShouldBe(3);
                votingResult.Results[options[0]].ShouldBe(100);
                votingResult.Results[options[1]].ShouldBe(100);
                votingResult.Results[options[2]].ShouldBe(200);

                //take snapshot
                var snapshotResult = await TakeSnapshot(registerItem.VotingItemId, 2);
                snapshotResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //query vote ids
                var user1VoteIds = await GetVoteIds(user1, registerItem.VotingItemId);
                user1VoteIds.ActiveVotes.Count.ShouldBe(1);
                user1VoteIds.WithdrawnVotes.Count.ShouldBe(1);

                var user2VoteIds = await GetVoteIds(user2, registerItem.VotingItemId);
                user2VoteIds.ActiveVotes.Count.ShouldBe(2);
                user2VoteIds.WithdrawnVotes.Count.ShouldBe(0);

                var user3VoteIds = await GetVoteIds(user3, registerItem.VotingItemId);
                user3VoteIds.ActiveVotes.Count.ShouldBe(3);
                user3VoteIds.WithdrawnVotes.Count.ShouldBe(0);
            }

            //phase 3
            {
                //take snapshot
                var snapshotResult = await TakeSnapshot(registerItem.VotingItemId, 3);
                snapshotResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult = await Vote(user2, registerItem.VotingItemId, registerItem.Options[0], 100);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Current voting item already ended").ShouldBeTrue();
            }
        }
    }
}