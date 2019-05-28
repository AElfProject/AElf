using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        public MappedState<Hash, VotingItem> VotingItems { get; set; }
        
        /// <summary>
        /// This hash is calculated by: voting_item_id & epoch_number
        /// </summary>
        public MappedState<Hash, VotingResult> VotingResults { get; set; }
        
        /// <summary>
        /// VoteId -> VotingRecord
        /// Usually VoteId is Context.TransactionId
        /// </summary>
        public MappedState<Hash, VotingRecord> VotingRecords { get; set; }
        
        /// <summary>
        /// Voter's Address -> VotedItems
        /// </summary>
        public MappedState<Address, VotedItems> VotedItemsMap { get; set; }
    }
}