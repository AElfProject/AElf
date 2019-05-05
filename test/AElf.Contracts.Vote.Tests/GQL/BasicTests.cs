using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Shouldly;
using Vote;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        [Fact]
        public async Task VoteContract_Initialize_NotByContractZero()
        {
            var transactionResult = (await VoteContractStub.InitialVoteContract.SendAsync(new InitialVoteContractInput
            {
                TokenContractSystemName = Hash.Generate(),
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Only zero contract can");
        }

        [Fact]
        public async Task VoteContract_Register_Again()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 10);
            var transactionResult = (await VoteContractStub.Register.SendAsync(new VotingRegisterInput
            {
                // Basically same as previous one.
                TotalSnapshotNumber = votingItem.TotalSnapshotNumber,
                StartTimestamp = votingItem.StartTimestamp,
                EndTimestamp = votingItem.EndTimestamp,
                Options = {votingItem.Options},
                AcceptedCurrency = votingItem.AcceptedCurrency,
                IsLockToken = votingItem.IsLockToken
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Voting item already exists");
        }

        [Fact]
        public async Task VoteContract_Register_CurrencyNotSupportVoting()
        {
            
        }

        [Fact]
        public async Task VoteContract_Vote_NotSuccess()
        {
            //did not find related vote event
            {
                var input = new VoteInput
                {
                    VotingItemId = Hash.Generate()
                };

                var transactionResult = (await VoteContractStub.Vote.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Voting item not found").ShouldBeTrue();
            }
            
            //without such option
            {
                var votingItem = await RegisterVotingItemAsync(100, 4, true, DefaultSender, 2);
                
                var input = new VoteInput
                {
                    VotingItemId = votingItem.VotingItemId,
                    Option = "Somebody"
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain($"Option {input.Option} not found");
            }
            
            //not enough token
            {
                var votingItemId = await RegisterVotingItemAsync(100, 4, true, DefaultSender, 2);
                
                var input = new VoteInput
                {
                    VotingItemId = votingItemId.VotingItemId,
                    Option = votingItemId.Options.First(),
                    Amount = 2000_000_000L
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }
        }
    }
}