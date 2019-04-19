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
        public MappedState<Address, Hash, Receipt> LockedTokenAmount { get; set; }
        public MappedState<Hash, Int64Value> ApprovedTokenAmount { get; set; }
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}