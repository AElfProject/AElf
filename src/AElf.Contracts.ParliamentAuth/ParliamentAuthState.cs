using AElf.Contracts.Consensus.AEDPoS;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }
        
        public SingletonState<Address> DefaultOrganizationAddress { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract {get; set; }    
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
    }
}