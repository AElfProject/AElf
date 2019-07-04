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
        /// <summary>
        /// Set the State.Initialized.value=true,means the contract of vote has been initialized;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty InitialVoteContract(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            return new Empty();
        }

        /// <summary>
        /// According to the VotingRegisterInput,register a VotingItem.
        /// Initialize the VotingItem and related VotingResults.
        /// </summary>
        /// <param name="input">VotingRegisterInput</param>
        /// <returns></returns>
        public override Empty Register(VotingRegisterInput input)
        {
            //Sender represents the transaction's sponsor
            var votingItemId = input.GetHash(Context.Sender);

            if (input.TotalSnapshotNumber == 0)
            {
                input.TotalSnapshotNumber = 1;
            }

            Assert(input.TotalSnapshotNumber > 0, "Total snapshot number must be greater than 0.");
            Assert(input.EndTimestamp > input.StartTimestamp, "Invalid active time.");

            if (input.EndTimestamp == new Timestamp {Seconds = long.MaxValue})
            {
                Assert(input.TotalSnapshotNumber != 1, "Cannot created endless voting event.");
            }

            InitializeDependentContracts();

            Assert(State.VotingItems[votingItemId] == null, "Voting item already exists.");

            Context.LogDebug(() => $"Voting item created by {Context.Sender}: {votingItemId.ToHex()}");

            //Judge the AcceptedCurrency is exist in the WhiteList of TokenContract.
            //Only the currency existed in TokenContact can be used for voting.
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

        /// <summary>
        /// Execute the Vote action,save the VoteRecords and update the VotingResults and the VotedItems
        /// Before Voting,the VotingItem's token must be locked,except the votes delegated to a contract.
        /// </summary>
        /// <param name="input">VoteInput</param>
        /// <returns></returns>
        public override Empty Vote(VoteInput input)
        {
            //the VotingItem is exist in state.
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
                //Voter just is the transaction sponsor
                input.Voter = Context.Sender;
                //VoteId just is the transaction ID;
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
            //save the VotingRecords into the state.
            State.VotingRecords[input.VoteId] = votingRecord;

            UpdateVotingResult(votingItem, input.Option, input.Amount);
            UpdateVotedItems(input.VoteId, votingRecord.Voter, votingItem);

            if (votingItem.IsLockToken)
            {
                // Lock voted token.
                State.TokenContract.Lock.Send(new LockInput
                {
                    Address = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    LockId = input.VoteId,
                    Amount = input.Amount,
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

        //Update the VotedItems,if it doesn't exist in State.VotedItemsMap,will create a new VotedItems.
        private void UpdateVotedItems(Hash voteId, Address voter, VotingItem votingItem)
        {
            var votedItems = State.VotedItemsMap[voter] ?? new VotedItems();
            if (votedItems.VotedItemVoteIds.ContainsKey(votingItem.VotingItemId.ToHex()))
            {
                votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()].ActiveVotes.Add(voteId);
            }
            else
            {
                votedItems.VotedItemVoteIds[votingItem.VotingItemId.ToHex()] =
                    new VotedIds
                    {
                        ActiveVotes = {voteId}
                    };
            }

            State.VotedItemsMap[voter] = votedItems;
        }

        /// <summary>
        /// Update the State.VotingResults.include the VotersCount,VotesAmount and the votes int the results[option]
        /// </summary>
        /// <param name="votingItem"></param>
        /// <param name="option"></param>
        /// <param name="amount"></param>
        private void UpdateVotingResult(VotingItem votingItem, string option, long amount)
        {
            // Update VotingResult based on this voting behaviour.
            var votingResultHash = GetVotingResultHash(votingItem.VotingItemId, votingItem.CurrentSnapshotNumber);
            var votingResult = State.VotingResults[votingResultHash];
            if (!votingResult.Results.ContainsKey(option))
            {
                votingResult.Results.Add(option, 0);
            }

            var currentVotes = votingResult.Results[option];
            votingResult.Results[option] = currentVotes.Add(amount);
            votingResult.VotersCount = votingResult.VotersCount.Add(1);
            votingResult.VotesAmount = votingResult.VotesAmount.Add(amount);
            State.VotingResults[votingResultHash] = votingResult;
        }

        /// <summary>
        /// Withdraw the Votes.
        /// first,mark the related record IsWithdrawn.
        /// second,delete the vote form ActiveVotes and add the vote to withdrawnVotes.
        /// finally,unlock the token that Locked in the VotingItem 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
                    Address = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    Amount = votingRecord.Amount,
                    LockId = input.VoteId,
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

        /// <summary>
        /// Add a option for corresponding VotingItem.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty AddOption(AddOptionInput input)
        {
            var votingItem = AssertVotingItem(input.VotingItemId);
            Assert(votingItem.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(!votingItem.Options.Contains(input.Option), "Option already exists.");
            votingItem.Options.Add(input.Option);
            State.VotingItems[votingItem.VotingItemId] = votingItem;
            return new Empty();
        }

        /// <summary>
        /// Delete a option for corresponding VotingItem
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Initialize the related contracts=>TokenContract;
        /// </summary>
        private void InitializeDependentContracts()
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
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