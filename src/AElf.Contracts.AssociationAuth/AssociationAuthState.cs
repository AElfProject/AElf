using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<Hash, ApprovedResult> Approved { get; set; }
    }
}