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
            var votingEvent = new VotingEvent
            {
                Sponsor = input.Sponsor,
                Topic = input.Topic
            };
            var votingEventHash = votingEvent.GetHash();

            Assert(State.VotingEvents[votingEventHash] != null, "Voting event not found.");
            votingEvent = State.VotingEvents[votingEventHash];
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

            var votingRecord = new VotingRecord
            {
                Topic = input.Topic,
                Sponsor = input.Sponsor,
                Amount = input.Amount,
                EpochNumber = votingEvent.CurrentEpoch,
                Option = input.Option,
                IsWithdrawn = false,
                VoteTimestamp = Context.CurrentBlockTime.ToTimestamp(),
                Voter = votingEvent.Delegated ? input.Voter : Context.Sender
            };
            State.VotingRecords[input.VoteId] = votingRecord;

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

            return new Empty();
        }

        public override Empty UpdateEpochNumber(UpdateEpochNumberInput input)
        {
            var votingEvent = new VotingEvent
            {
                Sponsor = Context.Sender,
                Topic = input.Topic
            };
            var votingEventHash = votingEvent.GetHash();

            Assert(State.VotingEvents[votingEventHash] != null, "Voting event not found.");

            votingEvent = State.VotingEvents[votingEventHash];

            votingEvent.CurrentEpoch = input.EpochNumber;

            State.VotingEvents[votingEventHash] = votingEvent;

            return new Empty();
        }

        public override VotingResult GetVotingInfo(GetVotingResultInput input)
        {
            var votingResultHash = Hash.FromMessage(input);
            return State.VotingResults[votingResultHash];
        }
    }
}