using Acs3;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthState : ContractState
    {
        public SingletonState<Hash> ParliamentAuthContractSystemName { get; set; }
        public SingletonState<Address> ParliamentAuthContractAddress { get; set; }
        public SingletonState<Hash> TokenContractAddressSystemName { get; set; }

        public SingletonState<Hash> AssociationAuthContractSystemName { get; set; }
        public BoolState Initialized { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<Hash, ApprovedResult> Approved { get; set; }

        public MappedState<Address, Hash, VoteInfo> LockedVoteAmount { get; set; }
        public MappedState<Hash, Int64Value> ApprovedVoteAmount { get; set; }
        
        internal SingletonState<TokenInfo> VoteTokenInfo { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}