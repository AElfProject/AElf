using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        /// <summary>
        /// This hash is calculated by: topic & sponsor
        /// VotingEventHash
        /// </summary>
        public MappedState<Hash, VotingEvent> VotingEvents { get; set; }
        
        /// <summary>
        /// This hash is calculated by: topic & sponsor & epoch_number
        /// VotingGoingHash
        /// </summary>
        public MappedState<Hash, VotingResult> VotingResults { get; set; }
        
        /// <summary>
        /// VoteId -> VotingRecord
        /// Usually VoteId is Context.TransactionId
        /// </summary>
        public MappedState<Hash, VotingRecord> VotingRecords { get; set; }
        
        /// <summary>
        /// Voter's Address -> VotingHistories
        /// </summary>
        public MappedState<Address, VotingHistories> VotingHistoriesMap { get; set; }
    }
}