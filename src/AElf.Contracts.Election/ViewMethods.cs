using System.Linq;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        public override PublicKeysList GetVictories(Empty input)
        {
            if (State.Candidates.Value == null || State.Candidates.Value.Value.Count <= State.MinersCount.Value)
            {
                return new PublicKeysList();
            }

            return new PublicKeysList
            {
                Value =
                {
                    State.Candidates.Value.Value.Select(p => p.ToHex()).Select(k => State.Votes[k])
                        .OrderByDescending(v => v.ValidObtainedVotesAmount).Select(v => v.PublicKey)
                        .Take(State.MinersCount.Value)
                }
            };
        }

        public override CandidateHistory GetCandidateHistory(StringInput input)
        {
            return State.Histories[input.Value];
        }

        public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
        {
            return State.Snapshots[input.TermNumber];
        }

        public override Votes GetTicketsInformation(StringInput input)
        {
            var votingRecords = State.VoteContract.GetVotingHistory.Call(new GetVotingHistoryInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Voter = Address.FromPublicKey(ByteArrayHelpers.FromHexString(input.Value))
            });

            var electionTickets = new Votes
            {

            };

            return electionTickets;
        }
    }
}