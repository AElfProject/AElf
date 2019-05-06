using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<Address, Hash, Receipt> LockedTokenAmount { get; set; }
        public MappedState<Hash, long> ApprovedTokenAmount { get; set; }
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}