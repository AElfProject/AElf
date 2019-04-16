using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            
            //sponsor is not sender
            {
                var input = new VoteInput
                {
                    Topic = "Topic1",
                    Sponsor = DefaultSender,
                };
            }
            
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
    }
}