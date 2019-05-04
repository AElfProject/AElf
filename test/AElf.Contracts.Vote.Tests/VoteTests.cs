using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Vote;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        private List<string> _options = new List<string>();
        public VoteTests()
        {
            InitializeContracts();
        }
        
        [Fact]
        public async Task VoteContract_InitializeMultiTimes()
        {
            var transactionResult = (await VoteContractStub.InitialVoteContract.SendAsync(new InitialVoteContractInput
            {
                TokenContractSystemName = Hash.Generate(),
            })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task VoteContract_RegisterSuccess()
        {
            _options = GenerateOptions(3);
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = 1,
                EndTimestamp = DateTime.UtcNow.AddDays(10).ToTimestamp(),
                StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                Options =
                {
                    _options
                },
                AcceptedCurrency = "ELF"
            };

            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //register again
            transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Voting item already exists");
        }

        [Fact]
        public async Task VoteContract_VoteFailed()
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
                var votingItemId = await RegisterVotingItem(100, 4, true, DefaultSender, 2);
                
                var input = new VoteInput
                {
                    VotingItemId = votingItemId,
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
                var votingItemId = await RegisterVotingItem(100, 4, true, DefaultSender, 2);
                
                var input = new VoteInput
                {
                    VotingItemId = votingItemId,
                    Option = _options[1],
                    Amount = 2000_000_000L
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }
        }

        [Fact]
        public async Task VoteContract_GetVotingResult()
        {
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            var votingItemId = await RegisterVotingItem(10, 3, true, DefaultSender, 2);
            await UserVote(voteUser, votingItemId, _options[1], 1000L);

            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                VotingItemId = votingItemId,
                SnapshotNumber = 1
            });
            
            votingResult.VotingItemId.ShouldBe(votingItemId);
            votingResult.VotersCount.ShouldBe(1);
            votingResult.Results.Values.First().ShouldBe(1000L);
        }

        private async Task<Hash> RegisterVotingItem(int lastingDays, int optionsCount, bool isLockToken, Address sender,
            int totalSnapshotNumber = int.MaxValue)
        {
            _options = GenerateOptions(optionsCount);
            var startTime = DateTime.UtcNow;

            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = totalSnapshotNumber,
                EndTimestamp = startTime.AddDays(lastingDays).ToTimestamp(),
                StartTimestamp = startTime.ToTimestamp(),
                Options =
                {
                    _options
                },
                AcceptedCurrency = "ELF",
                IsLockToken = isLockToken
            };
            await VoteContractStub.Register.SendAsync(input);
            return input.GetHash(sender);
        }

        private async Task<TransactionResult> UserVote(ECKeyPair voteUser, Hash votingItemId, string option,
            long amount)
        {
            var input = new VoteInput
            {
                VotingItemId = votingItemId,
                Option = option,
                Amount = amount
            };

            var voteUserStub = GetVoteContractTester(voteUser);
            var transactionResult = (await voteUserStub.Vote.SendAsync(input)).TransactionResult;

            return transactionResult;
        }

        private List<string> GenerateOptions(int count = 1)
        {
            var addressList = new List<string>(); 
            for (int i = 0; i < count; i++)
            {
                addressList.Add(Address.Generate().GetFormatted());
            }

            return addressList;
        }
        
        private async Task<long> GetUserBalance(byte[] publicKey)
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Address.FromPublicKey(publicKey)
            })).Balance;

            return balance;
        }
    }
}