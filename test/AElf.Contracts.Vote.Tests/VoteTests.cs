using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote
{
    public class VoteTests : VoteContractTestBase
    {
        private List<string> Options = new List<string>();
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
                ConsensusContractSystemName = Hash.Generate()
            })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_RegisterFailed()
        {
            //invalid topic
            {
                var input = new VotingRegisterInput
                {
                    Topic = string.Empty,
                    TotalEpoch = 1
                };
                
                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            } 
            
            //endless vote event
            {
                var input = new VotingRegisterInput
                {
                    Topic = "bp election topic",
                    TotalEpoch = 1,
                    ActiveDays = int.MaxValue
                };

                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Cannot created endless voting event").ShouldBeTrue(); 
            }
            
            //voting with not accepted currency
            {
                var input = new VotingRegisterInput
                {
                    Topic = "bp election topic",
                    TotalEpoch = 1,
                    ActiveDays = 100,
                    StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                    AcceptedCurrency = "UTC"
                };
                
                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Claimed accepted token is not available for voting").ShouldBeTrue(); 
            }
        }

        [Fact]
        public async Task VoteContract_RegisterSuccess()
        {
            Options = GenerateOptions(3);
            var input = new VotingRegisterInput
            {
                Topic = "Topic1",
                TotalEpoch = 1,
                ActiveDays = 100,
                StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                Options =
                {
                    Options
                },
                AcceptedCurrency = "ELF"
            };
            
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //register again
            transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting event already exists").ShouldBeTrue();
        }

        [Fact]
        public async Task VoteContract_VoteFailed()
        {
            //did not find related vote event
            {
                var input = new VoteInput
                {
                    Topic = "Not existed vote",
                    Sponsor = Address.Generate(),
                };

                var transactionResult = (await VoteContractStub.Vote.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Voting event not found").ShouldBeTrue();
            }
            
            //without such option
            {
                await GenerateNewVoteEvent("topic0", 2, 100, 4, true);
                
                var input = new VoteInput
                {
                    Topic = "topic0",
                    Sponsor = DefaultSender,
                    Option = "Somebody"
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains($"Option {input.Option} not found").ShouldBeTrue();
            }
            
            //not enough token
            {
                await GenerateNewVoteEvent("topic1", 2, 100, 4, false);
                
                var input = new VoteInput
                {
                    Topic = "topic1",
                    Sponsor = DefaultSender,
                    Option = Options[1],
                    Amount = 2000_000L
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
            }
        }

        private async Task<TransactionResult> GenerateNewVoteEvent(string topic, int totalEpoch, int activeDays, int optionCount, bool delegated)
        {
            Options = GenerateOptions(optionCount);
            var input = new VotingRegisterInput
            {
                Topic = topic,
                TotalEpoch = totalEpoch,
                ActiveDays = activeDays,
                StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                Options =
                {
                    Options
                },
                AcceptedCurrency = "ELF",
                Delegated = delegated
            };
            
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

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