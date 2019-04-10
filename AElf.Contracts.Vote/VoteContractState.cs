using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Hash, VotingEvent> VotingEvents { get; set; }
        public MappedState<Hash, VotingResult> VotingResults { get; set; }
        public MappedState<Hash, VotingRecord> VotingRecords { get; set; }
    }
}