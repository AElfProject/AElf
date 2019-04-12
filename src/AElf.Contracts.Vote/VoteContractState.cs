using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        // This hash is calculated by: topic & sponsor
        public MappedState<Hash, VotingEvent> VotingEvents { get; set; }
        
        // This hash is calculated by: topic & sponsor & epoch_number
        public MappedState<Hash, VotingResult> VotingResults { get; set; }
        
        // vote_id -> voting_record
        public MappedState<Hash, VotingRecord> VotingRecords { get; set; }
        public MappedState<Address, VotingHistories> VotingHistoriesMap { get; set; }
    }
}