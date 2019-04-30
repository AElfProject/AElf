using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Vote;

namespace AElf.Contracts.Vote
{
    /// <summary>
    /// Comments and documents see README.md of current project.
    /// </summary>
    public partial class VoteContract : VoteContractContainer.VoteContractBase
    {
        public override Empty InitialVoteContract(InitialVoteContractInput input)
        {
            Assert(Context.Sender == Context.GetZeroSmartContractAddress(),
                "Only zero contract can initialize this contract.");

            Assert(!State.Initialized.Value, "Already initialized.");

            State.TokenContractSystemName.Value = input.TokenContractSystemName;

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty Register(VotingRegisterInput input)
        {
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

            var votingItemHash = input.GetHash(Context.Sender);

            Assert(State.VotingItems[votingItemHash] == null, "Voting event already exists.");
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
                VotingItemId = votingItemHash,
                AcceptedCurrency = input.AcceptedCurrency,
                IsLockToken = input.IsLockToken,
                TotalSnapshotNumber = input.TotalSnapshotNumber,
                CurrentSnapshotNumber = 1,
                CurrentSnapshotStartTimestamp = Context.CurrentBlockTime.ToTimestamp(),
                StartTimestamp = input.StartTimestamp,
            };
            votingItem.Options.AddRange(input.Options);
            if (Context.CurrentHeight > 1)
            {
                votingItem.RegisterTimestamp = Context.CurrentBlockTime.ToTimestamp();
            }

            State.VotingItems[votingItemHash] = votingItem;

            // Initialize first voting going information of registered voting event.
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                VotingItemId = votingItemHash,
                SnapshotNumber = 1
            });
            State.VotingResults[votingResultHash] = new VotingResult
            {
                VotingItemId = votingItemHash,
                SnapshotNumber = 1,
                SnapshotStartTimestamp = input.StartTimestamp
            };

            return new Empty();
        }

        public override Empty Vote(VoteInput input)
        {
            var votingItem = State.VotingItems[input.VotingItemId];
            
            Assert(votingItem != null, "Voting item not found.");
            if (votingItem == null)
            {
                return new Empty();
            }
            Assert(votingItem.Options.Contains(input.Option), $"Option {input.Option} not found.");
            Assert(votingItem.CurrentSnapshotNumber <= votingItem.TotalSnapshotNumber, "Current voting item already ended.");
            if (votingItem.IsLockToken)
            {
                Assert(votingItem.Sponsor == Context.Sender, "Sender of delegated voting event must be the Sponsor.");
                Assert(input.Voter != null, "Voter cannot be null if voting event is delegated.");
                Assert(input.VoteId != null, "Vote Id cannot be null if voting event is delegated.");
            }
            else
            {
                input.VoteId = Context.TransactionId;
            }

            var votingRecord = new VotingRecord
            {
                VotingItemId = input.VotingItemId,
                Amount = input.Amount,
                SnapshotNumber = votingItem.CurrentSnapshotNumber,
                Option = input.Option,
                IsWithdrawn = false,
                VoteTimestamp = Context.CurrentBlockTime.ToTimestamp(),
                Voter = input.Voter
            };

            // Update VotingResult based on this voting behaviour.
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = votingItem.CurrentSnapshotNumber
            });
            var votingResult = State.VotingResults[votingResultHash];
            if (!votingResult.Results.ContainsKey(input.Option))
            {
                votingResult.Results.Add(input.Option, 0);
            }
            var currentVotes = votingResult.Results[input.Option];
            votingResult.Results[input.Option] = currentVotes + input.Amount;

            // Update voting history
            var votedItems = State.VotedItemsMap[votingRecord.Voter] ?? new VotedItems();
            votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Add(input.VoteId);

            State.VotingRecords[input.VoteId] = votingRecord;

            State.VotingResults[votingResultHash] = votingResult;

            State.VotedItemsMap[votingRecord.Voter] = votedItems;

            if (!votingItem.IsLockToken)
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

            Assert(votingItem.CurrentSnapshotNumber > votingRecord.SnapshotNumber,
                "Cannot withdraw votes of on-going voting item.");

            // Update VotingRecord.
            votingRecord.IsWithdrawn = true;
            votingRecord.WithdrawTimestamp = Context.CurrentBlockTime.ToTimestamp();
            State.VotingRecords[input.VoteId] = votingRecord;

            var votingResultHash = new VotingResult
            {
                VotingItemId = votingRecord.VotingItemId,
                SnapshotNumber = votingRecord.SnapshotNumber
            }.GetHash();
            
            var votedItems = State.VotedItemsMap[votingRecord.Voter];
            votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Remove(input.VoteId);
            votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].WithdrawnVotes.Add(input.VoteId);
            State.VotedItemsMap[votingRecord.Voter] = votedItems;

            var votingResult = State.VotingResults[votingResultHash];
            votingResult.Results[votingRecord.Option] -= votingRecord.Amount;
            if (!votedItems.VotedItemVoteIds[votingRecord.VotingItemId.ToHex()].ActiveVotes.Any())
            {
                votingResult.VotersCount -= 1;
            }

            State.VotingResults[votingResultHash] = votingResult;

            if (!State.VotingItems[votingRecord.VotingItemId].IsLockToken)
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

            return new Empty();
        }

        public override Empty TakeSnapshot(TakeSnapshotInput input)
        {
            var votingItem = State.VotingItems[input.VotingItemId];
            Assert(votingItem != null, "Voting item not found.");
            if (votingItem == null)
            {
                return new Empty();
            }
            Assert(votingItem.CurrentSnapshotNumber - 1 <= votingItem.TotalSnapshotNumber,
                "Current voting item already ended.");

            // Update previous voting going information.
            var previousVotingResultHash = new VotingResult
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = votingItem.CurrentSnapshotNumber
            }.GetHash();
            var previousVotingResult = State.VotingResults[previousVotingResultHash];
            previousVotingResult.SnapshotEndTimestamp = Context.CurrentBlockTime.ToTimestamp();
            State.VotingResults[previousVotingResultHash] = previousVotingResult;

            Assert(votingItem.CurrentSnapshotNumber + 1 == input.SnapshotNumber,
                $"Can only increase epoch number 1 each time: {votingItem.CurrentSnapshotNumber} -> {input.SnapshotNumber}");
            votingItem.CurrentSnapshotNumber = input.SnapshotNumber;
            State.VotingItems[votingItem.VotingItemId] = votingItem;

            // Initial next voting going information.
            var currentVotingGoingHash = new VotingResult
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = input.SnapshotNumber
            }.GetHash();
            State.VotingResults[currentVotingGoingHash] = new VotingResult
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = input.SnapshotNumber,
                SnapshotStartTimestamp = Context.CurrentBlockTime.ToTimestamp()
            };
            return new Empty();
        }

        public override VotingResult GetVotingResult(GetVotingResultInput input)
        {
            var votingResultHash = Hash.FromMessage(input);
            return State.VotingResults[votingResultHash];
        }

        public override Empty AddOption(AddOptionInput input)
        {
            var votingItem = State.VotingItems[input.VotingItemId];
            Assert(votingItem != null, "Voting item not found.");
            if (votingItem == null)
            {
                return new Empty();
            }
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(!votingItem.Options.Contains(input.Option), "Option already exists.");
            votingItem.Options.Add(input.Option);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        public override Empty RemoveOption(RemoveOptionInput input)
        {
            var votingItem = State.VotingItems[input.VotingItemId];
            Assert(votingItem != null, "Voting item not found.");
            if (votingItem == null)
            {
                return new Empty();
            }
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(votingItem.Options.Contains(input.Option), "Option doesn't exist.");
            votingItem.Options.Remove(input.Option);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        public override VotedItems GetVotedItems(Address input)
        {
            return State.VotedItemsMap[input] ?? new VotedItems();
        }

        public override VotingRecord GetVotingRecord(Hash input)
        {
            var votingRecord = State.VotingRecords[input];
            Assert(votingRecord != null, "Voting record not found.");
            return votingRecord;
        }
        

        public override VotingItem GetVotingItem(GetVotingItemInput input)
        {
            var votingEvent = State.VotingItems[input.VotingItemId];
            Assert(votingEvent != null, "Voting Event not found.");
            return votingEvent;
        }
        
        private void InitializeDependentContracts()
        {
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            }
        }
    }
}