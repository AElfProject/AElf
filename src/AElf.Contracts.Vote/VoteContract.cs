using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

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
            if (input.TotalEpoch == 0)
            {
                input.TotalEpoch = 1;
            }
            
            Assert(input.Topic != null, "Topic cannot be null or empty.");
            Assert(input.TotalEpoch > 0, "Total epoch number must be greater than 0.");
            Assert(input.ActiveDays > 0, "Total active days must be greater than 0.");

            if (input.ActiveDays == int.MaxValue)
            {
                Assert(input.TotalEpoch != 1, "Cannot created endless voting event.");
            }

            InitializeDependentContracts();

            if (input.StartTimestamp == null || input.StartTimestamp.ToDateTime() < Context.CurrentBlockTime)
            {
                input.StartTimestamp = Context.CurrentBlockTime.ToTimestamp();
            }

            var votingEvent = new VotingEvent
            {
                Sponsor = Context.Sender,
                Topic = input.Topic
            };
            var votingEventHash = votingEvent.GetHash();

            Assert(State.VotingEvents[votingEventHash] == null, "Voting event already exists.");
            var isInWhiteList = State.TokenContract.IsInWhiteList.Call(new IsInWhiteListInput
            {
                Symbol = input.AcceptedCurrency,
                Address = Context.Self
            }).Value;
            Assert(isInWhiteList, "Claimed accepted token is not available for voting.");

            // Initialize voting event.
            votingEvent.AcceptedCurrency = input.AcceptedCurrency;
            votingEvent.ActiveDays = input.ActiveDays;
            votingEvent.Delegated = input.Delegated;
            votingEvent.TotalEpoch = input.TotalEpoch;
            votingEvent.Options.AddRange(input.Options);
            votingEvent.CurrentEpoch = 1;
            votingEvent.EpochStartTimestamp = input.StartTimestamp;
            votingEvent.RegisterTimestamp = Context.CurrentBlockTime.ToTimestamp();
            votingEvent.StartTimestamp = input.StartTimestamp;

            State.VotingEvents[votingEventHash] = votingEvent;

            // Initialize first voting going information of registered voting event.
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                Sponsor = Context.Sender,
                Topic = input.Topic,
                EpochNumber = 1
            });
            State.VotingResults[votingResultHash] = new VotingResult
            {
                Topic = input.Topic,
                Sponsor = Context.Sender,
                EpochNumber = 1
            };

            return new Empty();
        }

        public override Empty Vote(VoteInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);

            Assert(votingEvent.Options.Contains(input.Option), $"Option {input.Option} not found.");
            Assert(votingEvent.CurrentEpoch <= votingEvent.TotalEpoch, "Current voting event already terminated.");
            if (votingEvent.Delegated)
            {
                Assert(input.Sponsor == Context.Sender, "Sender of delegated voting event must be the Sponsor.");
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
                Topic = input.Topic,
                Sponsor = input.Sponsor,
                Amount = input.Amount,
                EpochNumber = votingEvent.CurrentEpoch,
                Option = input.Option,
                IsWithdrawn = false,
                VoteTimestamp = Context.CurrentBlockTime.ToTimestamp(),
                Voter = input.Voter,
                Currency = votingEvent.AcceptedCurrency
            };

            // Update VotingResult based on this voting behaviour.
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                Sponsor = input.Sponsor,
                Topic = input.Topic,
                EpochNumber = votingEvent.CurrentEpoch
            });
            var votingResult = State.VotingResults[votingResultHash];
            if (!votingResult.Results.ContainsKey(input.Option))
            {
                votingResult.Results.Add(input.Option, 0);
            }
            var currentVotes = votingResult.Results[input.Option];
            votingResult.Results[input.Option] = currentVotes + input.Amount;

            // Update voting history
            var votingHistories = State.VotingHistoriesMap[votingRecord.Voter] ?? new VotingHistories
            {
                Voter = votingRecord.Voter
            };
            var votingEventHash = votingEvent.GetHash().ToHex();
            if (!votingHistories.Votes.ContainsKey(votingEventHash))
            {
                votingHistories.Votes[votingEventHash] = new VotingHistory
                {
                    ActiveVotes = {input.VoteId}
                };
                votingResult.VotersCount += 1;
            }
            else
            {
                votingHistories.Votes[votingEventHash].ActiveVotes.Add(input.VoteId);
            }

            State.VotingRecords[input.VoteId] = votingRecord;

            State.VotingResults[votingResultHash] = votingResult;

            State.VotingHistoriesMap[votingRecord.Voter] = votingHistories;

            if (!votingEvent.Delegated)
            {
                // Lock voted token.
                State.TokenContract.Lock.Send(new LockInput
                {
                    From = votingRecord.Voter,
                    Symbol = votingEvent.AcceptedCurrency,
                    LockId = input.VoteId,
                    Amount = input.Amount,
                    To = Context.Self,
                    Usage = $"Voting for {input.Topic}"
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
            var votingEventHash = new VotingEvent
            {
                Topic = votingRecord.Topic,
                Sponsor = votingRecord.Sponsor
            }.GetHash();

            var votingEvent = State.VotingEvents[votingEventHash];

            Assert(votingEvent.CurrentEpoch > votingRecord.EpochNumber,
                "Cannot withdraw votes of on-going voting event.");

            // Update VotingRecord.
            votingRecord.IsWithdrawn = true;
            votingRecord.WithdrawTimestamp = Context.CurrentBlockTime.ToTimestamp();
            State.VotingRecords[input.VoteId] = votingRecord;

            var votingGoingHash = new VotingResult
            {
                Sponsor = votingRecord.Sponsor,
                Topic = votingRecord.Topic,
                EpochNumber = votingRecord.EpochNumber
            }.GetHash();
            
            var votingHistories = UpdateHistoryAfterWithdrawing(votingRecord.Voter, votingEventHash, input.VoteId);

            var votingResult = State.VotingResults[votingGoingHash];
           votingResult.Results[votingRecord.Option] -= votingRecord.Amount;
            if (!votingHistories.Votes[votingEventHash.ToHex()].ActiveVotes.Any())
            {
                votingResult.VotersCount -= 1;
            }

            State.VotingResults[votingGoingHash] = votingResult;

            if (!State.VotingEvents[votingEventHash].Delegated)
            {
                State.TokenContract.Unlock.Send(new UnlockInput
                {
                    From = votingRecord.Voter,
                    Symbol = votingRecord.Currency,
                    Amount = votingRecord.Amount,
                    LockId = input.VoteId,
                    To = Context.Self,
                    Usage = $"Withdraw votes for {votingRecord.Topic}"
                });
            }

            return new Empty();
        }

        public override Empty UpdateEpochNumber(UpdateEpochNumberInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, Context.Sender);
            
            Assert(votingEvent.CurrentEpoch <= votingEvent.TotalEpoch + 1, "Current voting event already terminated.");

            // Update previous voting going information.
            var previousVotingGoingHash = new VotingResult
            {
                Sponsor = Context.Sender,
                Topic = input.Topic,
                EpochNumber = votingEvent.CurrentEpoch
            }.GetHash();
            var previousVotingResult = State.VotingResults[previousVotingGoingHash];
            previousVotingResult.EndTimestamp = Context.CurrentBlockTime.ToTimestamp();
            State.VotingResults[previousVotingGoingHash] = previousVotingResult;

            Assert(votingEvent.CurrentEpoch + 1 == input.EpochNumber, "Can only increase epoch number 1 each time.");
            votingEvent.CurrentEpoch = input.EpochNumber;
            State.VotingEvents[votingEvent.GetHash()] = votingEvent;

            // Initial next voting going information.
            var currentVotingGoingHash = new VotingResult
            {
                Sponsor = Context.Sender,
                Topic = input.Topic,
                EpochNumber = input.EpochNumber
            }.GetHash();
            State.VotingResults[currentVotingGoingHash] = new VotingResult
            {
                Sponsor = Context.Sender,
                Topic = input.Topic,
                EpochNumber = input.EpochNumber,
                StartTimestamp = Context.CurrentBlockTime.ToTimestamp()
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
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);
            Assert(votingEvent.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(!votingEvent.Options.Contains(input.Option), "Option already exists.");
            votingEvent.Options.Add(input.Option);
            State.VotingEvents[votingEvent.GetHash()] = votingEvent;
            return new Empty();
        }

        public override Empty RemoveOption(RemoveOptionInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);
            Assert(votingEvent.Sponsor == Context.Sender, "Only sponsor can update options.");
            Assert(votingEvent.Options.Contains(input.Option), "Option doesn't exist.");
            votingEvent.Options.Remove(input.Option);
            State.VotingEvents[votingEvent.GetHash()] = votingEvent;
            return new Empty();
        }

        public override VotingHistories GetVotingHistories(Address input)
        {
            return State.VotingHistoriesMap[input];
        }

        public override VotingRecord GetVotingRecord(Hash input)
        {
            var votingRecord = State.VotingRecords[input];
            Assert(votingRecord != null, "Voting record not found.");
            return votingRecord;
        }

        public override VotingEvent GetVotingEvent(GetVotingEventInput input)
        {
            var votingEventHash = new VotingEvent
            {
                Topic = input.Topic,
                Sponsor = input.Sponsor
            }.GetHash();
            var votingEvent = State.VotingEvents[votingEventHash];
            Assert(votingEvent != null, "Voting Event not found.");
            return votingEvent;
        }

        public override VotingHistory GetVotingHistory(GetVotingHistoryInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);
            var allVotes = State.VotingHistoriesMap[input.Voter];
            Assert(allVotes != null, "Voting record not found.");
            if (allVotes == null)
            {
                return new VotingHistory();
            }
            var votes = allVotes.Votes[votingEvent.GetHash().ToHex()];
            Assert(votes != null, "Voting record not found.");
            if (votes == null)
            {
                return new VotingHistory();
            }
            var activeVotes = votes.ActiveVotes;
            var withdrawnVotes = votes.WithdrawnVotes;
            return new VotingHistory
            {
                ActiveVotes = {activeVotes}, WithdrawnVotes = {withdrawnVotes}
            };
        }

        private VotingEvent AssertVotingEvent(Hash topic, Address sponsor)
        {
            var votingEvent = new VotingEvent
            {
                Topic = topic,
                Sponsor = sponsor
            };
            var votingEventHash = votingEvent.GetHash();
            Assert(State.VotingEvents[votingEventHash] != null, "Voting event not found.");
            return State.VotingEvents[votingEventHash];
        }

        private VotingHistories UpdateHistoryAfterWithdrawing(Address voter, Hash votingEventHash, Hash voteId)
        {
            var votingHistories = State.VotingHistoriesMap[voter];
            votingHistories.Votes[votingEventHash.ToHex()].ActiveVotes.Remove(voteId);
            votingHistories.Votes[votingEventHash.ToHex()].WithdrawnVotes.Add(voteId);
            State.VotingHistoriesMap[voter] = votingHistories;
            return votingHistories;
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