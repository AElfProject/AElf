using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.VirtualAddress;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote;

public partial class VoteTests
{
    [Fact]
    public async Task VoteContract_Register_Test()
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
    public async Task Register_With_Invalid_Timestamp()
    {
        var endTime = TimestampHelper.GetUtcNow();
        var input = new VotingRegisterInput
        {
            TotalSnapshotNumber = 0,
            EndTimestamp = endTime,
            StartTimestamp = endTime.AddDays(1),
            Options = { GenerateOptions() },
            AcceptedCurrency = TestTokenSymbol,
            IsLockToken = true
        };
        var transactionResult = (await VoteContractStub.Register.SendWithExceptionAsync(input)).TransactionResult;
        transactionResult.Error.ShouldContain("Invalid active time.");
    }

    [Fact]
    public async Task Register_With_Zero_Total_Snapshot_Test()
    {
        var votingItem = await RegisterVotingItemAsync(10, 4, true, DefaultSender, 0);
        votingItem.CurrentSnapshotNumber.ShouldBe(1);
        var votingResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
        {
            VotingItemId = votingItem.VotingItemId,
            SnapshotNumber = 1
        });
        votingResult.VotingItemId.ShouldBe(votingItem.VotingItemId);
        votingResult.SnapshotNumber.ShouldBe(1);
    }

    [Fact]
    public async Task VoteContract_Vote_Test()
    {
        //voting item not exist
        {
            var transactionResult =
                await VoteWithException(DefaultSenderKeyPair, HashHelper.ComputeFrom("hash"), string.Empty, 100);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting item not found").ShouldBeTrue();
        }

        //voting item have been out of date
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            await TakeSnapshot(registerItem.VotingItemId, 1);

            var voter = Accounts[11].KeyPair;
            var voteResult =
                await VoteWithException(voter, registerItem.VotingItemId, registerItem.Options[0], 100);
            voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
            voteResult.Error.Contains("Current voting item already ended").ShouldBeTrue();
        }

        //vote without enough token
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var voter = Accounts[31].KeyPair;
            var voteResult =
                await VoteWithException(voter, registerItem.VotingItemId, registerItem.Options[0], 100);
            voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        //vote option length is over the limit 1024
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var voter = Accounts[11].KeyPair;
            var option = new StringBuilder();
            option.Append('a', VoteContractConstant.OptionLengthLimit + 1);
            var voteResult = await VoteWithException(voter, registerItem.VotingItemId, option.ToString(), 100);
            voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
            voteResult.Error.Contains("Invalid input.").ShouldBeTrue();
        }

        //vote option not exist
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var voter = Accounts[11].KeyPair;
            var option = Accounts[3].Address.ToBase58();
            var voteResult = await VoteWithException(voter, registerItem.VotingItemId, option, 100);
            voteResult.Status.ShouldBe(TransactionResultStatus.Failed);
            voteResult.Error.Contains($"Option {option} not found").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Vote_Sender_Test()
    {
        var votingItem = await RegisterVotingItemAsync(10, 4, false, DefaultSender, 10);
        var otherVoter = GetVoteContractTester(Accounts[11].KeyPair);
        var voteRet = await otherVoter.Vote.SendWithExceptionAsync(new VoteInput
        {
            VotingItemId = votingItem.VotingItemId,
            Amount = 100,
            Option = votingItem.Options[1]
        });
        voteRet.TransactionResult.Error.ShouldContain("Sender of delegated voting event must be the Sponsor.");
    }

    [Fact]
    public async Task Vote_Success()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
        var voteItemId = registerItem.VotingItemId;
        var voter = Accounts[11];
        var voteAmount = 100;
        var beforeVoteBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = TestTokenSymbol,
            Owner = voter.Address
        });
        var transactionResult = await Vote(voter.KeyPair, voteItemId, registerItem.Options[1], voteAmount);
        transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var voteResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
        {
            SnapshotNumber = 1,
            VotingItemId = voteItemId
        });
        voteResult.Results[registerItem.Options[1]].ShouldBe(voteAmount);
        voteResult.VotesAmount.ShouldBe(voteAmount);
        voteResult.VotersCount.ShouldBe(1);
        var afterVoteBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = TestTokenSymbol,
            Owner = voter.Address
        });
        beforeVoteBalance.Balance.Sub(afterVoteBalance.Balance).ShouldBe(voteAmount);
        var voteItems = await VoteContractStub.GetVotedItems.CallAsync(voter.Address);
        voteItems.VotedItemVoteIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task VoteContract_Withdraw_Fail_Test()
    {
        //const long txFee = 1_00000000;
        //without vote
        {
            var withdrawResult =
                await WithdrawWithException(Accounts[1].KeyPair, HashHelper.ComputeFrom("hash1"));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Voting record not found").ShouldBeTrue();
        }

        //Within lock token withdraw with other person
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);

            var voteUser = Accounts[1].KeyPair;
            var voteAddress = Accounts[1].Address;
            var withdrawUser = Accounts[2].KeyPair;

            await Vote(voteUser, registerItem.VotingItemId, registerItem.Options[1], 100);
            await TakeSnapshot(registerItem.VotingItemId, 1);

            var voteIds = await GetVoteIds(voteUser, registerItem.VotingItemId);
            var beforeBalance = GetUserBalance(voteAddress);

            var transactionResult = await WithdrawWithException(withdrawUser, voteIds.ActiveVotes.First());

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("No permission to withdraw votes of others");

            var afterBalance = GetUserBalance(voteAddress);
            beforeBalance.ShouldBe(afterBalance); // Stay same
        }

        //Without lock token and withdrawn by other person
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, false, DefaultSender, 1);
            var withdrawUser = Accounts[2];
            var voteId = HashHelper.ComputeFrom("hash");
            await VoteContractStub.Vote.SendAsync(new VoteInput
            {
                VotingItemId = registerItem.VotingItemId,
                Voter = withdrawUser.Address,
                VoteId = voteId,
                Option = registerItem.Options[1],
                Amount = 100
            });
            await TakeSnapshot(registerItem.VotingItemId, 1);
            var voteIds = await GetVoteIds(withdrawUser.KeyPair, registerItem.VotingItemId);
            var transactionResult = await WithdrawWithException(withdrawUser.KeyPair, voteIds.ActiveVotes.First());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("No permission to withdraw votes of others");
        }
    }

    [Fact]
    public async Task VoteContract_Withdraw_Success_Test()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);

        var voteUser = Accounts[1].KeyPair;
        var voteAddress = Accounts[1].Address;
        var voteItemId = registerItem.VotingItemId;
        var voteAmount = 100;
        await Vote(voteUser, voteItemId, registerItem.Options[1], voteAmount);
        var voteIds = await GetVoteIds(voteUser, voteItemId);
        var currentVoteId = voteIds.ActiveVotes.First();
        var voteRecordBeforeWithdraw = await VoteContractStub.GetVotingRecord.CallAsync(currentVoteId);
        voteRecordBeforeWithdraw.IsWithdrawn.ShouldBe(false);
        var voteItems = await VoteContractStub.GetVotedItems.CallAsync(voteAddress);
        voteItems.VotedItemVoteIds[voteItemId.ToHex()].ActiveVotes.Count.ShouldBe(1);
        voteItems.VotedItemVoteIds[voteItemId.ToHex()].WithdrawnVotes.Count.ShouldBe(0);
        var voteResultBeforeWithdraw = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
        {
            SnapshotNumber = 1,
            VotingItemId = voteItemId
        });
        await TakeSnapshot(voteItemId, 1);


        var beforeBalance = GetUserBalance(voteAddress);
        var transactionResult = await Withdraw(voteUser, currentVoteId);
        transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        voteItems = await VoteContractStub.GetVotedItems.CallAsync(voteAddress);
        voteItems.VotedItemVoteIds[voteItemId.ToHex()].ActiveVotes.Count.ShouldBe(0);
        voteItems.VotedItemVoteIds[voteItemId.ToHex()].WithdrawnVotes.Count.ShouldBe(1);
        var voteRecordAfterWithdraw = await VoteContractStub.GetVotingRecord.CallAsync(currentVoteId);
        voteRecordAfterWithdraw.IsWithdrawn.ShouldBe(true);
        var voteResultAfterWithdraw = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
        {
            SnapshotNumber = 1,
            VotingItemId = voteItemId
        });
        voteResultBeforeWithdraw.VotesAmount.Sub(voteResultAfterWithdraw.VotesAmount).ShouldBe(voteAmount);
        voteResultBeforeWithdraw.Results[registerItem.Options[1]]
            .Sub(voteResultAfterWithdraw.Results[registerItem.Options[1]]).ShouldBe(voteAmount);
        voteResultBeforeWithdraw.VotersCount.Sub(1).ShouldBe(voteResultAfterWithdraw.VotersCount);
        var afterBalance = GetUserBalance(voteAddress);
        beforeBalance.ShouldBe(afterBalance - 100);
    }

    [Fact]
    public async Task VoteContract_AddOption_Fail_Test()
    {
        //vote item does not exist
        {
            var voteItemId = HashHelper.ComputeFrom("hash");
            var otherUser = Accounts[10].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).AddOption.SendWithExceptionAsync(
                new AddOptionInput
                {
                    Option = Accounts[0].Address.ToBase58(),
                    VotingItemId = voteItemId
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting item not found.").ShouldBeTrue();
        }
        //add without permission
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var otherUser = Accounts[10].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).AddOption.SendWithExceptionAsync(
                new AddOptionInput
                {
                    Option = Accounts[0].Address.ToBase58(),
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
        }

        //add duplicate option
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var transactionResult = (await VoteContractStub.AddOption.SendWithExceptionAsync(new AddOptionInput
            {
                Option = registerItem.Options[0],
                VotingItemId = registerItem.VotingItemId
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Option already exists").ShouldBeTrue();
        }

        // option length exceed 1024
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var newOption = new StringBuilder().Append('a', VoteContractConstant.OptionLengthLimit + 1);
            var transactionResult = (await VoteContractStub.AddOption.SendWithExceptionAsync(new AddOptionInput
            {
                Option = newOption.ToString(),
                VotingItemId = registerItem.VotingItemId
            })).TransactionResult;
            transactionResult.Error.ShouldContain("Invalid input.");
        }

        // option count exceed 64
        {
            var registerItem = await RegisterVotingItemAsync(100, VoteContractConstant.MaximumOptionsCount, true,
                DefaultSender, 1);
            var newOption = Accounts[VoteContractConstant.MaximumOptionsCount].Address.ToBase58();
            var transactionResult = (await VoteContractStub.AddOption.SendWithExceptionAsync(new AddOptionInput
            {
                Option = newOption,
                VotingItemId = registerItem.VotingItemId
            })).TransactionResult;
            transactionResult.Error.ShouldContain(
                $"The count of options can't greater than {VoteContractConstants.MaximumOptionsCount}");
        }
    }

    [Fact]
    public async Task VoteContract_AddOption_Success_Test()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
        var address = Accounts[3].Address.ToBase58();
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

    [Fact]
    public async Task VoteContract_RemoveOption_Fail_Test()
    {
        //voteItem does not exist
        {
            var voteItemId = HashHelper.ComputeFrom("hash");
            var transactionResult = (await VoteContractStub.RemoveOption.SendWithExceptionAsync(
                new RemoveOptionInput
                {
                    Option = Accounts[3].Address.ToBase58(),
                    VotingItemId = voteItemId
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Voting item not found.");
        }
        //remove without permission
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var otherUser = Accounts[10].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).RemoveOption.SendWithExceptionAsync(
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
            var transactionResult = (await VoteContractStub.RemoveOption.SendWithExceptionAsync(
                new RemoveOptionInput
                {
                    Option = Accounts[3].Address.ToBase58(),
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Option doesn't exist").ShouldBeTrue();
        }

        //option length exceed 1024
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var invalidOption = new StringBuilder().Append('a', VoteContractConstant.OptionLengthLimit + 1);
            var transactionResult = (await VoteContractStub.RemoveOption.SendWithExceptionAsync(
                new RemoveOptionInput
                {
                    Option = invalidOption.ToString(),
                    VotingItemId = registerItem.VotingItemId
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Invalid input.").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task VoteContract_RemoveOption_Success_Test()
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

    [Fact]
    public async Task VoteContract_AddOptions_Test()
    {
        //without permission
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var otherUser = Accounts[10].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).AddOptions.SendWithExceptionAsync(
                new AddOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        Accounts[0].Address.ToBase58(),
                        Accounts[1].Address.ToBase58()
                    }
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Only sponsor can update options").ShouldBeTrue();
        }
        //voteItem does not exist
        {
            var itemId = HashHelper.ComputeFrom("hash");
            var transactionResult = (await VoteContractStub.AddOptions.SendWithExceptionAsync(new AddOptionsInput
            {
                VotingItemId = itemId,
                Options =
                {
                    Accounts[0].Address.ToBase58()
                }
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Voting item not found.").ShouldBeTrue();
        }
        //success
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var transactionResult = (await VoteContractStub.AddOptions.SendAsync(new AddOptionsInput
            {
                VotingItemId = registerItem.VotingItemId,
                Options =
                {
                    Accounts[3].Address.ToBase58(),
                    Accounts[4].Address.ToBase58()
                }
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var votingItem = await GetVoteItem(registerItem.VotingItemId);
            votingItem.Options.Count.ShouldBe(5);
        }
    }

    [Fact]
    public async Task VoteContract_RemoveOptions_Fail_Test()
    {
        //voteItem does not exist
        {
            var voteItemId = HashHelper.ComputeFrom("hash");
            var transactionResult = (await VoteContractStub.RemoveOptions.SendWithExceptionAsync(
                new RemoveOptionsInput
                {
                    VotingItemId = voteItemId,
                    Options =
                    {
                        Accounts[0].Address.ToBase58()
                    }
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Voting item not found.");
        }
        //without permission
        {
            var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
            var otherUser = Accounts[10].KeyPair;
            var transactionResult = (await GetVoteContractTester(otherUser).RemoveOptions.SendWithExceptionAsync(
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
            var transactionResult = (await VoteContractStub.RemoveOptions.SendWithExceptionAsync(
                new RemoveOptionsInput
                {
                    VotingItemId = registerItem.VotingItemId,
                    Options =
                    {
                        registerItem.Options[0],
                        Accounts[0].Address.ToBase58()
                    }
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Option doesn't exist").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task VoteContract_RemoveOptions_Success_Test()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
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

    [Fact]
    public async Task VoteContract_VotesAndGetVotedItems_Test()
    {
        var voteUser = Accounts[2].KeyPair;
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

        await Vote(voteUser, votingItem.VotingItemId, votingItem.Options.Last(), 500L);
        var votedResult = await GetVotedItems(Address.FromPublicKey(voteUser.PublicKey));
        votedResult.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task VoteContract_GetLatestVotingResult_Test()
    {
        var voteUser1 = Accounts[2].KeyPair;
        var voteUser2 = Accounts[3].KeyPair;
        var votingItem = await RegisterVotingItemAsync(10, 3, true, DefaultSender, 2);

        await Vote(voteUser1, votingItem.VotingItemId, votingItem.Options.First(), 100L);
        await Vote(voteUser1, votingItem.VotingItemId, votingItem.Options.First(), 200L);
        var votingResult = await GetLatestVotingResult(votingItem.VotingItemId);
        votingResult.VotersCount.ShouldBe(2);
        votingResult.VotesAmount.ShouldBe(300L);

        await Vote(voteUser2, votingItem.VotingItemId, votingItem.Options.Last(), 100L);
        await Vote(voteUser2, votingItem.VotingItemId, votingItem.Options.Last(), 200L);
        votingResult = await GetLatestVotingResult(votingItem.VotingItemId);
        votingResult.VotersCount.ShouldBe(4);
        votingResult.VotesAmount.ShouldBe(600L);
    }

    [Fact]
    public async Task VoteContract_GetVotedItems_Default_Return_Test()
    {
        var address = Address.FromPublicKey(Accounts[1].KeyPair.PublicKey);
        var votedItem = await GetVotedItems(address);
        votedItem.ShouldBe(new VotedItems());
    }

    [Fact]
    public async Task VoteContract_GetVotingRecords_Test()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
        var voteUser = Accounts[1].KeyPair;
        var voteItemId = registerItem.VotingItemId;
        var voteAmount = 100;
        await Vote(voteUser, voteItemId, registerItem.Options[1], voteAmount);
        var voteIds = await GetVoteIds(voteUser, voteItemId);
        var currentVoteId = voteIds.ActiveVotes.First();
        var voteRecord = await VoteContractStub.GetVotingRecords.CallAsync(new GetVotingRecordsInput
        {
            Ids = { currentVoteId }
        });
        voteRecord.Records.Count.ShouldBe(1);
        voteRecord.Records[0].Amount.ShouldBe(voteAmount);
    }

    [Fact]
    public async Task VoteContract_GetVotingIds_Test()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
        var voteUser = Accounts[1];
        var voteItemId = registerItem.VotingItemId;
        var voteAmount = 100;
        await Vote(voteUser.KeyPair, voteItemId, registerItem.Options[1], voteAmount);
        var voteIds = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
        {
            Voter = voteUser.Address,
            VotingItemId = registerItem.VotingItemId
        });
        voteIds.ActiveVotes.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task Vote_VirtualAddress_Success()
    {
        var registerItem = await RegisterVotingItemAsync(100, 3, true, DefaultSender, 1);
        var voteItemId = registerItem.VotingItemId;
        var voter = await VirtualAddressContractStub.GetVirtualAddress.CallAsync(new Empty());
        var voteAmount = 100;
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Symbol = TestTokenSymbol,
            Amount = 1000,
            To = voter,
            Memo = "transfer token to voter"
        });
        var beforeVoteBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = TestTokenSymbol,
            Owner = voter
        });
        var sendResult = await VirtualAddressContractStub.ForwardCall.SendAsync(new ForwardCallInput
        {
            ContractAddress = VoteContractAddress,
            MethodName = "Vote",
            VirtualAddress = HashHelper.ComputeFrom("test"),
            Args = (new VoteInput
            {
                VotingItemId = voteItemId,
                Option = registerItem.Options[1],
                Amount = voteAmount
            }).ToByteString()
        });

        sendResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var voteResult = await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
        {
            SnapshotNumber = 1,
            VotingItemId = voteItemId
        });
        voteResult.Results[registerItem.Options[1]].ShouldBe(voteAmount);
        voteResult.VotesAmount.ShouldBe(voteAmount);
        voteResult.VotersCount.ShouldBe(1);
        var afterVoteBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = TestTokenSymbol,
            Owner = voter
        });
        beforeVoteBalance.Balance.Sub(afterVoteBalance.Balance).ShouldBe(voteAmount);
        var voteItems = await VoteContractStub.GetVotedItems.CallAsync(voter);
        voteItems.VotedItemVoteIds.Count.ShouldBe(1);
        
        var votingIds = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
        {
            Voter = voter,
            VotingItemId = voteItemId
        });
        var currentVoteId = votingIds.ActiveVotes.First();
        
        sendResult = await VirtualAddressContractStub.ForwardCall.SendAsync(new ForwardCallInput
        {
            ContractAddress = VoteContractAddress,
            MethodName = "Withdraw",
            VirtualAddress = HashHelper.ComputeFrom("test"),
            Args = (new WithdrawInput
            {
                VoteId = currentVoteId
            }).ToByteString()
        });
        sendResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var afterWithdrawBalance = GetUserBalance(voter);
        afterWithdrawBalance.ShouldBe(beforeVoteBalance.Balance);
    }
}