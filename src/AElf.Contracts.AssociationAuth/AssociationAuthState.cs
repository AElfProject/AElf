using Acs3;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthState : ContractState
    {
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<Hash, Approved> Approved { get; set; }
        public SingletonState<Association> Association { get; set; }
        public SingletonState<Hash> ParliamentAuthContractSystemName { get; set; }
        public BoolState Initialized { get; set; }
    }
}