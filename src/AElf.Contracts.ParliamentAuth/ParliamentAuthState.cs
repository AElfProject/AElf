using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }
        
        public SingletonState<Address> ZeroOwnerAddress { get; set; }
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract {get; set; }    
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
    }
}