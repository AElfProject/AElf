using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    public class VoteContract : VoteContractContainer.VoteContractBase
    {
        public override Empty Register(VotingRegisterInput input)
        {
            var votingEvent = new VotingEvent
            {
                Sponsor = input.Sponsor,
                Topic = input.Topic
            };
            var votingEventHash = votingEvent.GetHash();

            Assert(State.VotingEvents[votingEventHash] == null, "Voting event already exists.");
            Assert(input.TotalEpoch >= 1, "Invalid total epoch.");
            // TODO: if input.Delegated == false, check white list of accepted symbol.

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
                Sponsor = input.Sponsor,
                Topic = input.Topic,
                EpochNumber = 1
            });
            State.VotingResults[votingResultHash] = new VotingResult
            {
                Topic = input.Topic,
                Sponsor = input.Sponsor
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
            
            Assert(State.VotingEvents[votingEventHash] != null, "No such voting event.");
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
            
            return new Empty();
        }

        public override Empty UpdateEpochNumber(UpdateEpochNumberInput input)
        {
            return base.UpdateEpochNumber(input);
        }
    }
}