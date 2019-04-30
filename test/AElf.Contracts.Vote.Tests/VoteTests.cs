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
    public class VoteTests : VoteContractTestBase
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
        public async Task VoteContract_RegisterFailed()
        {
            //invalid topic
            {
                var input = new VotingRegisterInput
                {
                    TotalSnapshotNumber = 1
                };
                
                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Topic cannot be null or empty").ShouldBeTrue(); 
            }
            
            //endless vote event
            {
                var input = new VotingRegisterInput
                {
                    TotalSnapshotNumber = 1,
                    EndTimestamp = DateTime.UtcNow.ToUniversalTime().ToTimestamp(),
                    Options =
                    {
                        Address.Generate().GetFormatted()
                    }
                };

                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Cannot created endless voting event").ShouldBeTrue(); 
            }
            
            //voting with not accepted currency
            {
                var input = new VotingRegisterInput
                {
                    TotalSnapshotNumber = 1,
                    EndTimestamp = DateTime.UtcNow.ToUniversalTime().ToTimestamp(),
                    StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                    AcceptedCurrency = "UTC",
                    Options =
                    {
                        Address.Generate().GetFormatted()
                    }
                };
                
                var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Claimed accepted token is not available for voting").ShouldBeTrue(); 
            }
        }
/*

        [Fact]
        public async Task VoteContract_RegisterSuccess()
        {
            _options = GenerateOptions(3);
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = 1,
                EndTimestamp = DateTime.UtcNow.ToUniversalTime().ToTimestamp(),
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
            transactionResult.Error.Contains("Voting event already exists").ShouldBeTrue();
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
                transactionResult.Error.Contains("Voting event not found").ShouldBeTrue();
            }
            
            //without such option
            {
                await GenerateNewVoteEvent(Hash.FromString("topic0"), 2, 100, 4, true);
                
                var input = new VoteInput
                {
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
                await GenerateNewVoteEvent(Hash.FromString("topic1"), 2, 100, 4, false);
                
                var input = new VoteInput
                {
                    VotingItemId = 
                    Option = _options[1],
                    Amount = 2000_000L
                };
                var otherKeyPair = SampleECKeyPairs.KeyPairs[1];
                var otherVoteStub = GetVoteContractTester(otherKeyPair);
                
                var transactionResult = (await otherVoteStub.Vote.SendAsync(input)).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task VoteContract_VoteSuccess()
        {
            await GenerateNewVoteEvent(Hash.FromString("topic1"), 2, 100, 4, false);

            var voteAmount = 10_000L;
            var voteUser = SampleECKeyPairs.KeyPairs[1]; 
            var beforeBalance = await GetUserBalance(voteUser.PublicKey);
                
            var input = new VoteInput
            {
                Topic = Hash.FromString("topic1"),
                Sponsor = DefaultSender,
                Option = _options[1],
                Amount = voteAmount
            };
            var voteUserStub = GetVoteContractTester(voteUser);
                
            var transactionResult = (await voteUserStub.Vote.SendAsync(input)).TransactionResult;
                
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = await GetUserBalance(voteUser.PublicKey);
            beforeBalance.ShouldBe(afterBalance + voteAmount);
        }

        [Fact]
        public async Task VoteContract_AddOption()
        {
            var topic = Hash.FromString("vote test");
            await GenerateNewVoteEvent(topic, 1, 10, 1, false);
            
            //operate with not sponsor
            {
                var otherVoteStub = GetVoteContractTester(SampleECKeyPairs.KeyPairs[1]);
                var option = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey).GetFormatted();

                var transactionResult = (await otherVoteStub.AddOption.SendAsync(new AddOptionInput
                {
                    Option = option,
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }
            //add exist
            {
                var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
                {
                    Option = _options[0],
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option already exists").ShouldBeTrue();
            }
            //success
            {
                var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
                {
                    Option = Address.Generate().GetFormatted(),
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task VoteContract_RemoveOption()
        {
            var topic = Hash.FromString("vote test");
            await GenerateNewVoteEvent(topic, 1, 10, 1, false);
            
            //operate with not sponsor
            {
                var otherVoteStub = GetVoteContractTester(SampleECKeyPairs.KeyPairs[1]);
                var option = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey).GetFormatted();

                var transactionResult = (await otherVoteStub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    Option = option,
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }
            //remove not exist
            {
                var transactionResult = (await VoteContractStub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    Option = Address.Generate().GetFormatted(),
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option doesn't exist").ShouldBeTrue();
            }
            //success
            {
                var transactionResult = (await VoteContractStub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    Option = _options[0],
                    Sponsor = DefaultSender,
                    Topic = topic,
                })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task VoteContract_GetVotingHistory()
        {
            var topic = Hash.FromString("vote test");
            var voteUsrer = SampleECKeyPairs.KeyPairs[2];
            await GenerateNewVoteEvent(topic, 1, 10, 3, false);
            
            //voting event not exist
            {
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(
                    new GetVotingHistoryInput
                    {
                        Topic = Hash.FromString("test1"),
                        Sponsor = DefaultSender,
                        Voter = Address.Generate()
                    });
                votingHistory.ActiveVotes.Count.ShouldBe(0);
                
            }
            //voting without result
            {
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(
                    new GetVotingHistoryInput
                    {
                        Topic = topic,
                        Sponsor = DefaultSender,
                        Voter = Address.FromPublicKey(voteUsrer.PublicKey)
                    });
                votingHistory.ActiveVotes.Count.ShouldBe(0);

            }
            //success
            {
                await UserVote(voteUsrer, topic, DefaultSender, _options[0], 200);
                await UserVote(voteUsrer, topic, DefaultSender, _options[1], 800);
                
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(
                    new GetVotingHistoryInput
                    {
                        Topic = topic,
                        Sponsor = DefaultSender,
                        Voter = Address.FromPublicKey(voteUsrer.PublicKey)
                    });
                votingHistory.ActiveVotes.Count.ShouldBe(2);
            }
        }
        
        [Fact]
        public async Task VoteContract_GetVotingHistories()
        {
            var topic = Hash.FromString("vote test");
            await GenerateNewVoteEvent(topic, 1, 10, 3, false);
            
            //without vote
            {
                var votes = (await VoteContractStub.GetVotingHistories.CallAsync(
                    Address.Generate())).Votes;
                
                votes.Count.ShouldBe(0);
            }
            //with one vote
            {
                var voteUser = SampleECKeyPairs.KeyPairs[2];
                await UserVote(voteUser, topic, DefaultSender, _options[0], 1000L);
                
                var votes = (await VoteContractStub.GetVotingHistories.CallAsync(
                    Address.FromPublicKey(voteUser.PublicKey))).Votes;
                
                votes.Values.First().ActiveVotes.Count.ShouldBe(1);
            }
            //with multiple votes
            {
                var voteUser = SampleECKeyPairs.KeyPairs[2];
                await UserVote(voteUser, topic, DefaultSender, _options[1], 1000L);
                
                var votes = (await VoteContractStub.GetVotingHistories.CallAsync(
                    Address.FromPublicKey(voteUser.PublicKey))).Votes;
                
                votes.Values.First().ActiveVotes.Count.ShouldBe(2);
            }
        }

        [Fact]
        public async Task QueryVotingRecord()
        {
            var topic = Hash.FromString("vote test");
            await GenerateNewVoteEvent(topic, 1, 10, 3, false);
            
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            await UserVote(voteUser, topic, DefaultSender, _options[0], 1000L);
            
            var votes = (await VoteContractStub.GetVotingHistories.CallAsync(
                Address.FromPublicKey(voteUser.PublicKey))).Votes;
            var hash = votes.Values.First().ActiveVotes.First();
            
            //with data
            {
                var record = await VoteContractStub.GetVotingRecord.CallAsync(hash);
                
                record.Topic.ShouldBe(topic);
                record.Amount.ShouldBe(1000L);
                record.Option.ShouldBe(_options[0]);
                record.Voter.ShouldBe(Address.FromPublicKey(voteUser.PublicKey));
            }
            
            //without data
            {
                var record = await VoteContractStub.GetVotingRecord.CallAsync(Hash.Generate());
                
                record.ShouldBe(new VotingRecord());
            }
        }

        [Fact]
        public async Task VoteContract_UpdateEpochNumber()
        {
            var topic = Hash.FromString("vote test");
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            //totalEpoch is 1
            {
                await GenerateNewVoteEvent(topic, 1, 10, 3, false);
                await UserVote(voteUser, topic, DefaultSender, _options[1], 1000L);

                var transactionResult = (await VoteContractStub.UpdateEpochNumber.SendAsync(
                    new UpdateEpochNumberInput
                    {
                        Topic = topic,
                        EpochNumber = 2
                    })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var votingResult = await VoteContractStub.GetVotingResult.CallAsync(
                    new GetVotingResultInput
                    {
                        Topic = topic,
                        Sponsor = DefaultSender,
                        EpochNumber = 2
                    });
                votingResult.EpochNumber.ShouldBe(2);
                votingResult.EndTimestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToTimestamp());
            }
        }

        [Fact]
        public async Task VoteContract_Withdraw()
        {
            //not exist voting record
            {
                var voteUser = SampleECKeyPairs.KeyPairs[1];
                var voteUserStub = GetVoteContractTester(voteUser);
                
                var transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = Hash.Generate()
                    })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Voting record not found").ShouldBeTrue();
            }
            //withdraw ongoing event
            {
                var topic = Hash.FromString("test2");
                var voteUser = SampleECKeyPairs.KeyPairs[2];
                var voteUserStub = GetVoteContractTester(voteUser);
                await GenerateNewVoteEvent(topic, 1, 100, 2, false);
                await UserVote(voteUser, topic, DefaultSender, _options[0], 200);

                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(new GetVotingHistoryInput
                {
                    Topic = topic,
                    Sponsor = DefaultSender,
                    Voter = Address.FromPublicKey(voteUser.PublicKey)
                });
                
                var transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = votingHistory.ActiveVotes.First()
                    })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Cannot withdraw votes of on-going voting event").ShouldBeTrue();
            }
            
            //withdraw one completed event
            {
                var topic = Hash.FromString("test3");
                var voteUser = SampleECKeyPairs.KeyPairs[3];
                var voteAmount = 500L;
                var voteUserStub = GetVoteContractTester(voteUser);
                
                await GenerateNewVoteEvent(topic, 1, 100, 2, false);
                await UserVote(voteUser, topic, DefaultSender, _options[0], voteAmount);
                
                var beforeBalance = await GetUserBalance(voteUser.PublicKey);
                
                await VoteContractStub.UpdateEpochNumber.SendAsync(new UpdateEpochNumberInput
                {
                    Topic = topic,
                    EpochNumber = 2
                });
                
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(new GetVotingHistoryInput
                {
                    Topic = topic,
                    Sponsor = DefaultSender,
                    Voter = Address.FromPublicKey(voteUser.PublicKey)
                });
                
                var transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = votingHistory.ActiveVotes.First()
                    })).TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = await GetUserBalance(voteUser.PublicKey);
                afterBalance.ShouldBe(beforeBalance + voteAmount);
            }
            
            //withdraw multiple times of completed event
            {
                var topic = Hash.FromString("test4");
                var voteUser = SampleECKeyPairs.KeyPairs[4];
                var voteAmount = 500L;
                var voteUserStub = GetVoteContractTester(voteUser);
                
                await GenerateNewVoteEvent(topic, 2, 100, 2, false);
                await UserVote(voteUser, topic, DefaultSender, _options[0], voteAmount);
                await UserVote(voteUser, topic, DefaultSender, _options[1], voteAmount);
                
                var beforeBalance = await GetUserBalance(voteUser.PublicKey);
                
                await VoteContractStub.UpdateEpochNumber.SendAsync(new UpdateEpochNumberInput
                {
                    Topic = topic,
                    EpochNumber = 2
                });
                
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(new GetVotingHistoryInput
                {
                    Topic = topic,
                    Sponsor = DefaultSender,
                    Voter = Address.FromPublicKey(voteUser.PublicKey)
                });
                
                var transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = votingHistory.ActiveVotes.First()
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = votingHistory.ActiveVotes.Last()
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var afterBalance = await GetUserBalance(voteUser.PublicKey);
                afterBalance.ShouldBe(beforeBalance + voteAmount*2);
            }
            
            //withdraw all tokens after event completed
            {
                var topic = Hash.FromString("test5");
                var voteUser = SampleECKeyPairs.KeyPairs[5];
                var voteAmount = 500L;
                var voteUserStub = GetVoteContractTester(voteUser);
                
                await GenerateNewVoteEvent(topic, 3, 100, 2, false);
                await UserVote(voteUser, topic, DefaultSender, _options[0], voteAmount);
                await UserVote(voteUser, topic, DefaultSender, _options[1], voteAmount);
                
                await VoteContractStub.UpdateEpochNumber.SendAsync(new UpdateEpochNumberInput
                {
                    Topic = topic,
                    EpochNumber = 2
                });
                
                await UserVote(voteUser, topic, DefaultSender, _options[1], voteAmount);
                
                var beforeBalance = await GetUserBalance(voteUser.PublicKey);
                
                var votingHistory = await VoteContractStub.GetVotingHistory.CallAsync(new GetVotingHistoryInput
                {
                    Topic = topic,
                    Sponsor = DefaultSender,
                    Voter = Address.FromPublicKey(voteUser.PublicKey)
                });
                
                votingHistory.ActiveVotes.Count.ShouldBe(3);
                
                var transactionResult = (await voteUserStub.Withdraw.SendAsync(
                    new WithdrawInput
                    {
                        VoteId = votingHistory.ActiveVotes.Last()
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                
                await VoteContractStub.UpdateEpochNumber.SendAsync(new UpdateEpochNumberInput
                {
                    Topic = topic,
                    EpochNumber = 3
                });

                foreach (var hash in votingHistory.ActiveVotes)
                {
                    transactionResult = (await voteUserStub.Withdraw.SendAsync(
                        new WithdrawInput
                        {
                            VoteId = hash
                        })).TransactionResult;
                    transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
                
                var afterBalance = await GetUserBalance(voteUser.PublicKey);
                afterBalance.ShouldBe(beforeBalance + voteAmount*3);
            }
        }

        [Fact]
        public void TestHash()
        {
            var hash = Hash.FromMessage(new VotingResult
            {
                Topic = Hash.FromString("test3"),
                Sponsor = DefaultSender,
                EpochNumber = 0
            });
            
        }

        [Fact]
        public async Task GetVotingEvent()
        {
            await GenerateNewVoteEvent(Hash.FromString("topic1"), 2, 100, 4, false);
            
            //without result
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Sponsor = DefaultSender,
                    Topic = Hash.FromString("topic")
                });
                
                votingEvent.ShouldBe(new VotingEvent());
            }
            //with result
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Sponsor = DefaultSender,
                    Topic = Hash.FromString("topic1")
                });
                
                votingEvent.ShouldNotBeNull();
                votingEvent.Topic.ShouldBe(Hash.FromString("topic1"));
                votingEvent.Sponsor.ShouldBe(DefaultSender);
                votingEvent.Options.ShouldBe(_options);
            }
        }
        
        [Fact]
        public async Task VoteContract_GetVotingResult()
        {
            var topic = Hash.FromString("vote test");
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            await GenerateNewVoteEvent(topic, 1, 10, 3, false);
            await UserVote(voteUser, topic, DefaultSender, _options[1], 1000L);

            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                Topic = topic,
                Sponsor = DefaultSender,
                EpochNumber = 1
            });
            
            votingResult.Topic.ShouldBe(topic);
            votingResult.VotersCount.ShouldBe(1);
            votingResult.Results.Values.First().ShouldBe(1000L);
        }
        
        private async Task<TransactionResult> GenerateNewVoteEvent(Hash topic, int totalEpoch, int activeDays, int optionCount, bool delegated)
        {
            _options = GenerateOptions(optionCount);
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = totalEpoch,
                ActiveDays = activeDays,
                StartTimestamp = DateTime.UtcNow.ToTimestamp(),
                Options =
                {
                    _options
                },
                AcceptedCurrency = "ELF",
                Delegated = delegated
            };
            
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;

            return transactionResult;
        }

        private async Task<TransactionResult> UserVote(ECKeyPair voteUser, Hash topic, Address sponsor, string option, long amount)
        {
            var input = new VoteInput
            {
                Topic = topic,
                Sponsor = sponsor,
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
        */
    }
}