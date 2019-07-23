using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {

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
            var startTime = TimestampHelper.GetUtcNow();
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = 5,
                EndTimestamp = startTime.AddDays(100),
                StartTimestamp = startTime,
                Options =
                {
                    GenerateOptions(3)
                },
                AcceptedCurrency = "USDT",
                IsLockToken = true
            };
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed); 
            transactionResult.Error.Contains("Claimed accepted token is not available for voting").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_Vote_NotSuccess()
        {
            //did not find related vote event
            {
                var input = new VoteInput
                {
                    VotingItemId = Hash.FromString("hash")
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