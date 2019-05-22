using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    /// <summary>
    /// Comments and documents see README.md of current project.
    /// </summary>
    public partial class VoteContract : VoteContractContainer.VoteContractBase
    {
        public override Empty InitialVoteContract(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty Register(VotingRegisterInput input)
        {
            var votingItemId = input.GetHash(Context.Sender);

            if (input.TotalSnapshotNumber == 0)
            {
                input.TotalSnapshotNumber = 1;
            }

            Assert(input.TotalSnapshotNumber > 0, "Total snapshot number must be greater than 0.");
            Assert(input.EndTimestamp > input.StartTimestamp, "Invalid active time.");

            if (input.EndTimestamp == DateTime.MaxValue.ToUniversalTime().ToTimestamp())
            {
                Assert(input.TotalSnapshotNumber != 1, "Cannot created endless voting event.");
            }

            InitializeDependentContracts();

            Assert(State.VotingItems[votingItemId] == null, "Voting item already exists.");

            Context.LogDebug(() => $"Voting item created by {Context.Sender}: {votingItemId.ToHex()}");

            var isInWhiteList = State.TokenContract.IsInWhiteList.Call(new IsInWhiteListInput
            {
                Symbol = input.AcceptedCurrency,
                Address = Context.Self
            }).Value;
            Assert(isInWhiteList, "Claimed accepted token is not available for voting.");

            // Initialize voting event.
            var votingItem = new VotingItem
            {
                Sponsor = Context.Sender,
                VotingItemId = votingItemId,
                AcceptedCurrency = input.AcceptedCurrency,
                IsLockToken = input.IsLockToken,
                TotalSnapshotNumber = input.TotalSnapshotNumber,
                CurrentSnapshotNumber = 1,
                CurrentSnapshotStartTimestamp = input.StartTimestamp,
                StartTimestamp = input.StartTimestamp,
                EndTimestamp = input.EndTimestamp,
                RegisterTimestamp = Context.CurrentBlockTime,
                Options = {input.Options}
            };

            State.VotingItems[votingItemId] = votingItem;

            // Initialize first voting going information of registered voting event.
            var votingResultHash = GetVotingResultHash(votingItemId, 1);
            State.VotingResults[votingResultHash] = new VotingResult
            {
                VotingItemId = votingItemId,
                SnapshotNumber = 1,
                SnapshotStartTimestamp = input.StartTimestamp
            };

            return new Empty();
        }

        public override Empty Vote(VoteInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Options.Contains(input.Option), $"Option {input.Option} not found.");
            Assert(votingItem.CurrentSnapshotNumber <= votingItem.TotalSnapshotNumber,
                "Current voting item already ended.");
            if (!votingItem.IsLockToken)
            {
                Assert(votingItem.Sponsor == Context.Sender, "Sender of delegated voting event must be the Sponsor.");
                Assert(input.Voter != null, "Voter cannot be null if voting event is delegated.");
                Assert(input.VoteId != null, "Vote Id cannot be null if voting event is delegated.");
            }
            else
            {
                input.Voter = Context.Sender;
                input.VoteId = Context.TransactionId;
            }

            var votingRecord = new VotingRecord
            {
                VotingItemId = input.VotingItemId,
                Amount = input.Amount,
                SnapshotNumber = votingItem.CurrentSnapshotNumber,
                Option = input.Option,
                IsWithdrawn = false,
                VoteTimestamp = Context.CurrentBlockTime,
                Voter = input.Voter
            };

            // Update VotingResult based on this voting behaviour.
            var votingResultHash = GetVotingResultHash(input.VotingItemId, votingItem.CurrentSnapshotNumber);
            var votingResult = State.VotingResults[votingResultHash];
            if (!votingResult.Results.ContainsKey(input.Option))
            {
                votingResult.Results.Add(input.Option, 0);
            }

            var currentVotes = votingResult.Results[input.Option];
            votingResult.Results[input.Option] = currentVotes.Add(input.Amount);
            votingResult.VotersCount = votingResult.VotersCount.Add(1);
            votingResult.VotesAmount = votingResult.VotesAmount.Add(input.Amount);

            // Update voted items information.
            var votedItems = State.VotedItemsMap[votingRecord.Voter] ?? new VotedItems();
            if (votedItems.VotedItemVoteIds.ContainsKey(votingItem.VotingItemId.ToHex()))
            {
                votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Add(input.VoteId);
            }
            else
            {
                votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()] =
                    new VotedIds
                    {
                        ActiveVotes = {input.VoteId}
                    };
            }

            State.VotingRecords[input.VoteId] = votingRecord;

            State.VotingResults[votingResultHash] = votingResult;

            State.VotedItemsMap[votingRecord.Voter] = votedItems;

            if (votingItem.IsLockToken)
            {
                // Lock voted token.
                State.TokenContract.Lock.Send(new LockInput
                {
                    From = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    LockId = input.VoteId,
                    Amount = input.Amount,
                    To = Context.Self,
                    Usage = $"Voting for {input.VotingItemId}"
                });
            }

            Context.Fire(new Voted
            {
                VoteId = input.VoteId,
                VotingItemId = votingRecord.VotingItemId,
                Voter = votingRecord.Voter,
                Amount = votingRecord.Amount,
                Option = votingRecord.Option,
                SnapshotNumber = votingRecord.SnapshotNumber,
                VoteTimestamp = votingRecord.VoteTimestamp
            });

            return new Empty();
        }

        public override Empty Withdraw(WithdrawInput input)
        {
            var votingRecord = State.VotingRecords[input.VoteId];
            Assert(votingRecord != null, "Voting record not found.");
            if (votingRecord == null)
            {
                return new Empty();
            }

            var votingItem = State.VotingItems[votingRecord.VotingItemId];

            if (votingItem.IsLockToken)
            {
                Assert(votingRecord.Voter == Context.Sender, "No permission to withdraw votes of others.");
            }

            Assert(votingItem.CurrentSnapshotNumber > votingRecord.SnapshotNumber,
                "Cannot withdraw votes of on-going voting item.");

            // Update VotingRecord.
            votingRecord.IsWithdrawn = true;
            votingRecord.WithdrawTimestamp = Context.CurrentBlockTime;
            State.VotingRecords[input.VoteId] = votingRecord;

            var votingResultHash = GetVotingResultHash(votingRecord.VotingItemId, votingRecord.SnapshotNumber);

            var votedItems = State.VotedItemsMap[votingRecord.Voter];
            votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Remove(input.VoteId);
            votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].WithdrawnVotes.Add(input.VoteId);
            State.VotedItemsMap[votingRecord.Voter] = votedItems;

            var votingResult = State.VotingResults[votingResultHash];
            votingResult.Results[votingRecord.Option] -= votingRecord.Amount;
            if (!votedItems.VotedItemVoteIds[votingRecord.VotingItemId.ToHex()].ActiveVotes.Any())
            {
                votingResult.VotersCount = votingResult.VotersCount.Sub(1);
            }

            votingResult.VotesAmount = votingResult.VotesAmount.Sub(votingRecord.Amount);

            State.VotingResults[votingResultHash] = votingResult;

            if (votingItem.IsLockToken)
            {
                State.TokenContract.Unlock.Send(new UnlockInput
                {
                    From = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    Amount = votingRecord.Amount,
                    LockId = input.VoteId,
                    To = Context.Self,
                    Usage = $"Withdraw votes for {votingRecord.VotingItemId}"
                });
            }

            Context.Fire(new Withdrawn
            {
                VoteId = input.VoteId
            });

            return new Empty();
        }

        public override Empty TakeSnapshot(TakeSnapshotInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);

            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can take snapshot.");

            Assert(votingItem.CurrentSnapshotNumber - 1 <= votingItem.TotalSnapshotNumber,
                "Current voting item already ended.");

            // Update previous voting going information.
            var previousVotingResultHash = GetVotingResultHash(input.VotingItemId, votingItem.CurrentSnapshotNumber);
            var previousVotingResult = State.VotingResults[previousVotingResultHash];
            previousVotingResult.SnapshotEndTimestamp = Context.CurrentBlockTime;
            State.VotingResults[previousVotingResultHash] = previousVotingResult;

            Assert(votingItem.CurrentSnapshotNumber == input.SnapshotNumber,
                $"Can only take snapshot of current snapshot number: {votingItem.CurrentSnapshotNumber}, but {input.SnapshotNumber}");
            var nextSnapshotNumber = input.SnapshotNumber.Add(1);
            votingItem.CurrentSnapshotNumber = nextSnapshotNumber;
            State.VotingItems[votingItem.VotingItemId] = votingItem;

            // Initial next voting going information.
            var currentVotingGoingHash = GetVotingResultHash(input.VotingItemId, nextSnapshotNumber);
            State.VotingResults[currentVotingGoingHash] = new VotingResult
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = nextSnapshotNumber,
                SnapshotStartTimestamp = Context.CurrentBlockTime,
                VotersCount = previousVotingResult.VotersCount,
                VotesAmount = previousVotingResult.VotesAmount
            };
            return new Empty();
        }

        public override Empty AddOption(AddOptionInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(!votingItem.Options.Contains(input.Option), "Option already exists.");
            votingItem.Options.Add(input.Option);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        public override Empty RemoveOption(RemoveOptionInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(votingItem.Options.Contains(input.Option), "Option doesn't exist.");
            votingItem.Options.Remove(input.Option);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        public override Empty AddOptions(AddOptionsInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            foreach (var option in input.Options)
            {
                Assert(!votingItem.Options.Contains(option), "Option already exists.");
            }

            votingItem.Options.AddRange(input.Options);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        public override Empty RemoveOptions(RemoveOptionsInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            foreach (var option in input.Options)
            {
                Assert(votingItem.Options.Contains(option), "Option doesn't exist.");
                votingItem.Options.Remove(option);
            }

            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        private VotingItem AssertVotingItem(Hash votingItemId)
        {
            var votingItem = State.VotingItems[votingItemId];
            Assert(votingItem != null, $"Voting item not found. {votingItemId.ToHex()}");
            return votingItem;
        }

        private void InitializeDependentContracts()
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }
        }

        private Hash GetVotingResultHash(Hash votingItemId, long snapshotNumber)
        {
            return new VotingResult
            {
                VotingItemId = votingItemId,
                SnapshotNumber = snapshotNumber
            }.GetHash();
        }
    }
}