using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Shouldly;
using Xunit;
using System;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        [Fact]
        public async Task VoteContract_Register()
        {
            var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 10);

            // Check voting item according to the input.
            (votingItem.EndTimestamp.ToDateTime() - votingItem.StartTimestamp.ToDateTime()).TotalDays.ShouldBe(10);
            votingItem.Options.Count.ShouldBe(4);
            votingItem.Sponsor.ShouldBe(DefaultSender);
            votingItem.TotalSnapshotNumber.ShouldBe(10);

            // Check more about voting item.
            votingItem.CurrentSnapshotNumber.ShouldBe(1);
            votingItem.CurrentSnapshotStartTimestamp.ShouldBe(votingItem.StartTimestamp);
            votingItem.RegisterTimestamp.ShouldBeGreaterThan(votingItem
                .StartTimestamp); // RegisterTimestamp should be a bit later.

            // Check voting result of first period initialized.
            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                VotingItemId = votingItem.VotingItemId,
                SnapshotNumber = 1
            });
            votingResult.VotingItemId.ShouldBe(votingItem.VotingItemId);
            votingResult.SnapshotNumber.ShouldBe(1);
            votingResult.SnapshotStartTimestamp.ShouldBe(votingItem.StartTimestamp);
            votingResult.SnapshotEndTimestamp.ShouldBe(null);
            votingResult.Results.Count.ShouldBe(0);
            votingResult.VotersCount.ShouldBe(0);
        }

        [Fact]
        public async Task VoteContract_Vote()
        {
            //voting item not exist
            {
                var transactionResult = await Vote(DefaultSenderKeyPair, Hash.FromString("hash"), string.Empty, 100);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Voting item not found").ShouldBeTrue();
            }

            //voting item have been out of date
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                await TakeSnapshot(registerItem.VotingItemId, 1);

                var voter = SampleECKeyPairs.KeyPairs[11];
                var voteResult = await Vote(voter, registerItem.VotingItemId, registerItem.Options[0], 100);
                voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
                voteResult.Error.Contains("Current voting item already ended").ShouldBeTrue();
            }

            //vote without enough token
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var voter = SampleECKeyPairs.KeyPairs[31];
                var voteResult = await Vote(voter, registerItem.VotingItemId, registerItem.Options[0], 100);
                voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
                voteResult.Error.Contains("Insufficient balance").ShouldBeTrue();
            }

            //vote option not exist
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var voter = SampleECKeyPairs.KeyPairs[11];
                var option = Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted();
                var voteResult = await Vote(voter, registerItem.VotingItemId, option, 100);
                voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
                voteResult.Error.Contains($"Option {option} not found").ShouldBeTrue();
            }

            //vote success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var voter = SampleECKeyPairs.KeyPairs[11];
                var voteResult = await Vote(voter, registerItem.VotingItemId, registerItem.Options[1], 100);
                voteResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task VoteContract_Withdraw()
        {
            //without vote
            {
                var withdrawResult = await Withdraw(SampleECKeyPairs.KeyPairs[1], Hash.FromString("hash1"));
                withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
                withdrawResult.Error.Contains("Voting record not found").ShouldBeTrue();
            }

            //withdraw with other person
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);

                var voteUser = SampleECKeyPairs.KeyPairs[1];
                var voteAddress = Address.FromPublicKey(voteUser.PublicKey);
                var withdrawUser = SampleECKeyPairs.KeyPairs[2];

                await Vote(voteUser, registerItem.VotingItemId, registerItem.Options[1], 100);
                await TakeSnapshot(registerItem.VotingItemId, 1);

                var voteIds = await GetVoteIds(voteUser, registerItem.VotingItemId);
                var beforeBalance = GetUserBalance(voteAddress);

                var transactionResult = await Withdraw(withdrawUser, voteIds.ActiveVotes.First());

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("No permission to withdraw votes of others");

                var afterBalance = GetUserBalance(voteAddress);
                beforeBalance.ShouldBe(afterBalance); // Stay same
            }

            //vote but not expire
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);

                var voteUser = SampleECKeyPairs.KeyPairs[1];

                await Vote(voteUser, registerItem.VotingItemId, registerItem.Options[1], 100);

                var voteIds = await GetVoteIds(voteUser, registerItem.VotingItemId);
                var transactionResult = await Withdraw(voteUser, voteIds.ActiveVotes.First());

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Cannot withdraw votes of on-going voting item").ShouldBeTrue();
            }

            //success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);

                var voteUser = SampleECKeyPairs.KeyPairs[1];
                var voteAddress = Address.FromPublicKey(voteUser.PublicKey);

                await Vote(voteUser, registerItem.VotingItemId, registerItem.Options[1], 100);
                await TakeSnapshot(registerItem.VotingItemId, 1);

                var voteIds = await GetVoteIds(voteUser, registerItem.VotingItemId);
                var beforeBalance = GetUserBalance(voteAddress);
                var transactionResult = await Withdraw(voteUser, voteIds.ActiveVotes.First());

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = GetUserBalance(voteAddress);
                beforeBalance.ShouldBe(afterBalance - 100);
            }
        }

        [Fact]
        public async Task VoteContract_AddOption()
        {
            //add without permission
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await GetVoteContractTester(otherUser).AddOption.SendAsync(new AddOptionInput
                {
                    Option = Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted(),
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }

            //add duplicate option
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
                {
                    Option = registerItem.Options[0],
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option already exists").ShouldBeTrue();
            }

            //add success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var address = Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted();
                var transactionResult = (await VoteContractStub.AddOption.SendAsync(new AddOptionInput
                {
                    Option = address,
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingItem = await GetVoteItem(registerItem.VotingItemId);
                votingItem.Options.Count.ShouldBe(4);
                votingItem.Options.Contains(address).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task VoteContract_RemoveOption()
        {
            //remove without permission
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await GetVoteContractTester(otherUser).RemoveOption.SendAsync(
                    new RemoveOptionInput
                    {
                        Option = registerItem.Options[0],
                        VotingItemId = registerItem.VotingItemId
                    })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }

            //remove not exist one
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var transactionResult = (await VoteContractStub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    Option = Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted(),
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option doesn't exist").ShouldBeTrue();
            }

            //remove success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var removeOption = registerItem.Options[0];
                var transactionResult = (await VoteContractStub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    Option = removeOption,
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingItem = await GetVoteItem(registerItem.VotingItemId);
                votingItem.Options.Count.ShouldBe(2);
                votingItem.Options.Contains(removeOption).ShouldBeFalse();
            }
        }

        [Fact]
        public async Task VoteContract_AddOptions()
        {
            //without permission
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await GetVoteContractTester(otherUser).AddOptions.SendAsync(new AddOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted(),
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted()
                    }
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }
            //with some of exist
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await VoteContractStub.AddOptions.SendAsync(new AddOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted(),
                        registerItem.Options[1]
                    }
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option already exists").ShouldBeTrue();
            }
            //success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await VoteContractStub.AddOptions.SendAsync(new AddOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted(),
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted()
                    }
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingItem = await GetVoteItem(registerItem.VotingItemId);
                votingItem.Options.Count.ShouldBe(5);
            }
        }

        [Fact]
        public async Task VoteContract_RemoveOptions()
        {
            //without permission
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await GetVoteContractTester(otherUser).RemoveOptions.SendAsync(
                    new RemoveOptionsInput
                    {
                        VotingItemId = registerItem.VotingItemId,
                        Options =
                        {
                            registerItem.Options[0],
                            registerItem.Options[1]
                        }
                    })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
            }
            //with some of not exist
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await VoteContractStub.RemoveOptions.SendAsync(new RemoveOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        registerItem.Options[0],
                        Address.FromBytes(Guid.NewGuid().ToByteArray().ComputeHash()).GetFormatted()
                    }
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Option doesn't exist").ShouldBeTrue();
            }
            //success
            {
                var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
                var otherUser = SampleECKeyPairs.KeyPairs[10];
                var transactionResult = (await VoteContractStub.RemoveOptions.SendAsync(new RemoveOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        registerItem.Options[0],
                        registerItem.Options[1]
                    }
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var votingItem = await GetVoteItem(registerItem.VotingItemId);
                votingItem.Options.Count.ShouldBe(1);
            }
        }

        [Fact]
        public async Task VoteContract_GetVotingResult()
        {
            var voteUser = SampleECKeyPairs.KeyPairs[2];
            var votingItem = await RegisterVotingItemAsync(10, 3, true, DefaultSender, 2);
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