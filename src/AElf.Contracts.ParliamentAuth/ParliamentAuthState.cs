using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }
        
        public SingletonState<Address> DefaultOrganizationAddress { get; set; }
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        internal Acs4.ACS4Container.ACS4ReferenceState ConsensusContract {get; set; }    
        internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
    }
}