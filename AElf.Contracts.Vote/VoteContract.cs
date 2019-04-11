using System.Linq;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    public class VoteContract : VoteContractContainer.VoteContractBase
    {
        public override Empty InitialVoteContract(InitialVoteContractInput input)
        {
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.TokenContractSystemName.Value = input.TokenContractSystemName;

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty Register(VotingRegisterInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            }

            var votingEvent = new VotingEvent
            {
                Sponsor = Context.Sender,
                Topic = input.Topic
            };
            var votingEventHash = votingEvent.GetHash();

            Assert(State.VotingEvents[votingEventHash] == null, "Voting event already exists.");
            Assert(input.TotalEpoch >= 1, "Invalid total epoch.");
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.AcceptedCurrency
            });
            Assert(tokenInfo.LockWhiteList.Contains(Context.Self),
                "Claimed accepted token is not available for voting.");

            // Initialize VotingEvent.
            votingEvent.AcceptedCurrency = input.AcceptedCurrency;
            votingEvent.ActiveDays = input.ActiveDays;
            votingEvent.Delegated = input.Delegated;
            votingEvent.TotalEpoch = input.TotalEpoch;
            votingEvent.Options.AddRange(input.Options);
            votingEvent.CurrentEpoch = 1;
            State.VotingEvents[votingEventHash] = votingEvent;

            // Initialize VotingResult of Epoch 1.
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                Sponsor = Context.Sender,
                Topic = input.Topic,
                EpochNumber = 1
            });
            State.VotingResults[votingResultHash] = new VotingResult
            {
                Topic = input.Topic,
                Sponsor = Context.Sender
            };

            return new Empty();
        }

        public override Empty Vote(VoteInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);
            
            // Modify VotingResult
            var votingResultHash = Hash.FromMessage(new GetVotingResultInput
            {
                Sponsor = input.Sponsor,
                Topic = input.Topic,
                EpochNumber = votingEvent.CurrentEpoch
            });
            var votingResult = State.VotingResults[votingResultHash];
            var currentVotes = votingResult.Results[input.Option];
            votingResult.Results[input.Option] = currentVotes + input.Amount;
            State.VotingResults[votingResultHash] = votingResult;

            // VoteId -> VotingRecord
            var votingRecord = new VotingRecord
            {
                Topic = input.Topic,
                Sponsor = input.Sponsor,
                Amount = input.Amount,
                EpochNumber = votingEvent.CurrentEpoch,
                Option = input.Option,
                IsWithdrawn = false,
                VoteTimestamp = Context.CurrentBlockTime.ToTimestamp(),
                Voter = votingEvent.Delegated ? input.Voter : Context.Sender,
                Currency = votingEvent.AcceptedCurrency
            };
            State.VotingRecords[input.VoteId] = votingRecord;
            
            // Update voting history
            var currentHistory = State.VotingHistories[votingRecord.Voter] ?? new VotingHistory
            {
                Voter = votingRecord.Voter,
            };
            currentHistory.History[votingEvent.GetHash().ToHex()].Values.Add(input.VoteId);
            State.VotingHistories[votingRecord.Voter] = currentHistory;

            // Lock voted token.
            State.TokenContract.Lock.Send(new LockInput
            {
                From = votingRecord.Voter,
                Symbol = votingEvent.AcceptedCurrency,
                LockId = input.VoteId,
                Amount = input.Amount,
                To = input.Sponsor,
                Usage = $"Voting for {input.Topic}"
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
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = votingRecord.Voter,
                Symbol = votingRecord.Currency,
                Amount = votingRecord.Amount,
                LockId = input.VoteId,
                To = votingRecord.Sponsor,
                Usage = $"Withdraw votes for {votingRecord.Topic}"
            });
            return new Empty();
        }

        public override Empty UpdateEpochNumber(UpdateEpochNumberInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, Context.Sender);
            votingEvent.CurrentEpoch = input.EpochNumber;
            State.VotingEvents[votingEvent.GetHash()] = votingEvent;
            return new Empty();
        }

        public override VotingResult GetVotingInfo(GetVotingResultInput input)
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

        public override VotingHistory GetVotingHistories(Address input)
        {
            return State.VotingHistories[input];
        }

        public override HashList GetVotingHistory(GetVotingHistoryInput input)
        {
            var votingEvent = AssertVotingEvent(input.Topic, input.Sponsor);
            return State.VotingHistories[input.Voter].History[votingEvent.GetHash().ToHex()];
        }

        private VotingEvent AssertVotingEvent(string topic, Address sponsor)
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
    }
}