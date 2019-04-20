using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }
        
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract {get; set; }    
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
    }
}