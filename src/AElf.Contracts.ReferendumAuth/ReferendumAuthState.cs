using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ProposalContract;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Address, Hash, VoteInfo> LockedVoteAmount { get; set; }
        public MappedState<Hash, Int64Value> ApprovedVoteAmount { get; set; }
        
        internal SingletonState<TokenInfo> VoteTokenInfo { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, BoolValue> ProposalReleaseStatus { get; set; }

        public SingletonState<Hash> TokenContractSystemName { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        
        public SingletonState<Hash> ProposalContractSystemName { get; set; }
        internal ProposalContractContainer.ProposalContractReferenceState ProposalContract { get; set; }        


    }
}